using AutoMapper;
using EmailFileService.Entities;
using EmailFileService.Exception;
using EmailFileService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IEmailService
    {
        void Register(RegisterUserDto dto);
        string SendEmail(Email email, IFormFile file);
        string Login(LoginDto dto);
        IEnumerable<ShowMyFilesDto> GetMyFiles();
        DownloadFileDto DownloadFile(string fileName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IFileEncryptDecryptService _encrypt;
        private readonly Authentication _authentication;
        private readonly IMapper _mapper;
        private readonly IUserServiceAccessor _userServiceAccessor;

        public EmailService(EmailServiceDbContext dbContext, IPasswordHasher<User> hasher,
            IFileEncryptDecryptService encrypt, Authentication authentication, IMapper mapper,
            IUserServiceAccessor userServiceAccessor)
        {
            _dbContext = dbContext;
            _hasher = hasher;
            _encrypt = encrypt;
            _authentication = authentication;
            _mapper = mapper;
            _userServiceAccessor = userServiceAccessor;
        }

        public void Register(RegisterUserDto dto)
        {
            var mainDirectory = GeneratePath(dto.Email);
            var fileKey = GenerateKey();
            var newUser = new User()
            {
                Email = dto.Email,
                Directories = new List<UserDirectory>()
                {
                    new UserDirectory()
                    {
                        DirectoryPath = mainDirectory
                    }
                },
                Keys = new Keys()
                {
                    Key = fileKey
                }
            };
            var passwordHash = _hasher.HashPassword(newUser, dto.Password);
            
            newUser.PasswordHash = passwordHash;

            Directory.CreateDirectory($"{GetDirectoryToSaveUsersFiles()}{mainDirectory}");

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

        }

        public string SendEmail(Email email, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {

                var mainDirectory = _userServiceAccessor.GetMainDirectory;
                var directoryWithUser = $"{GetDirectoryToSaveUsersFiles()}{mainDirectory}/";

                var fileName = file.FileName;
                int result;
                if (email.Title is not null)
                {
                    result = AddFileToDb(email.Sender, email.Title, fileName, out var fullPath);
                    directoryWithUser += fullPath;
                }
                else
                {
                    result = AddFileToDb(email.Sender, "", fileName, out var fullPath);
                    directoryWithUser += fullPath;
                }

                Directory.CreateDirectory(directoryWithUser.Replace("/" + fileName, ""));

                using (var writer = new FileStream(directoryWithUser, FileMode.Create))
                {
                    file.CopyTo(writer);
                }

                string nameOfCode = result switch
                {
                    -1 => "You have This file in this Directory",
                    1 => "Save to Db was successful",
                    2 => "File already exist, overwrite it?",
                    _ => "Something is wrong!"
                };

                _encrypt.FileEncrypt(directoryWithUser);
                return nameOfCode;
            }
            else throw new FileNotFoundException("Bad Request");
        }

        public string Login(LoginDto dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user is null) throw new ForbidException("Email or password isn't correct!");

            var isCorrect = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (isCorrect == PasswordVerificationResult.Failed)
                throw new ForbidException("Email or password isn't correct!");

            var claims = GenerateClaim(user.Email, GeneratePath(dto.Email), user.Id);

            return claims;
        }

        private string GenerateClaim(string email, string userMainPath, int id)
        {
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("MainDirectory", userMainPath)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authentication.JwtIssuer));
            var cred = new SigningCredentials(key,
                SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(_authentication.JwtExpire);

            var token = new JwtSecurityToken(_authentication.JwtIssuer,
                _authentication.JwtIssuer,
                claim,
                expires: expires,
                signingCredentials: cred);

            var tokenHandler = new JwtSecurityTokenHandler();

            return tokenHandler.WriteToken(token);
        }

        public IEnumerable<ShowMyFilesDto> GetMyFiles()
        {
            var userId = _userServiceAccessor.GetId;
            var files = _dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(e => e.Directories)
                .Select(com => new ShowMyFilesDto()
                {
                    UserDirectories = com.DirectoryPath,
                    Files = com.Files.Select(c => c.NameOfFile).AsEnumerable()
                });

            if (files is null) throw new System.Exception("You don't have any files");

            var userFilesDto = files;

            return userFilesDto;
        }

        public DownloadFileDto DownloadFile(string fileName)
        {
            var mainDirectory = GetDirectoryToSaveUsersFiles();
            var userDirectory = _userServiceAccessor.GetMainDirectory;
            var userId = _userServiceAccessor.GetId;
            var user = _dbContext.Users
                .Include(c => c.Directories)
                .ThenInclude(c => c.Files)
                .FirstOrDefault(d => d.Id == userId);

            if (user is null) throw new NotFoundException("");

            var userDirectories = user.Directories.ToList();
            var userFiles = userDirectories.SelectMany(f => f.Files).FirstOrDefault(f => f.NameOfFile == fileName);

            var cos = userDirectories.FirstOrDefault(f => f.Files.Contains(userFiles));

            if (userFiles is null) throw new NotFoundException("Login user don't have this file!");

            var fullPath = $"{mainDirectory}{userDirectory}/{cos.DirectoryPath}/{fileName}";

            var ex = Path.GetExtension(fullPath).ToLowerInvariant();

            _encrypt.FileDecrypt(fullPath);

            return new DownloadFileDto()
            {
                ExtensionFile = GetTypeOfFile()[ex],
                PathToFile = fullPath
            };
        }

        private static string GeneratePath(string email)
        {
            var path = "";

            if (email.Length > 0)
            {
                path = email.Replace('@', '_').Replace('.', '_');
            }

            return path;
        }

        private string GetDirectoryToSaveUsersFiles() => Directory.GetCurrentDirectory() + "/UserDirectory/";

        private Dictionary<string, string> GetTypeOfFile()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"},
            };
        }

        private static readonly Random random = new Random();

        private static string GenerateKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private int AddFileToDb(string email, string? dictionaryName, string fileName, out string fullPath)
        {
            var user = _dbContext.Users
                .Include(u => u.Directories)
                .ThenInclude(ud => ud.Files)
                .AsSplitQuery()
                .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (user is null) throw new NotFoundException("We don't have this email");

            var dictionary = user?.Directories.ToList();

            if (dictionaryName.Length > 0)
            {
                var result = AddFileToDirectory(ref dictionary, dictionaryName, fileName);
                if (result < 0)
                {
                    fullPath = $"{dictionaryName}/{fileName}";
                    return result;
                }
                fullPath = $"{dictionaryName}/{fileName}";
                user.Directories = dictionary;
                _dbContext.SaveChanges();
                return 1;
            }
            else
            {
                var mainDirectory = dictionary.FirstOrDefault(d => d.DirectoryPath == _userServiceAccessor.GetMainDirectory);
                var result = AddFileToDirectory(ref mainDirectory, _userServiceAccessor.GetMainDirectory, fileName);
                if (result < 0)
                {
                    fullPath = $"{dictionaryName}/{fileName}";
                    return result;
                }
                user.Directories = dictionary;
                fullPath = $"/{fileName}";
                _dbContext.SaveChanges();
                return 1;
            }
        }

        private static int AddFileToDirectory(ref List<UserDirectory> userDirectory, string dictionary, string nameOfFile) // Dodawanie do nie istniejącego UserDirectory
        {
            var checkIfThisFileExist = UserHaveThisFileInThisDirectory(userDirectory, dictionary, file: nameOfFile);
            var thisDictionary = userDirectory.FirstOrDefault(d => d.DirectoryPath == dictionary);
            if (thisDictionary is null)
            {
                CreateUserDirectory(ref userDirectory, dictionary);
            }
            var newDirectory = userDirectory.FirstOrDefault(d => d.DirectoryPath == dictionary);
            if (checkIfThisFileExist is false)
            {
                var newFile = newDirectory?.Files.Append(new Entities.File()
                {
                    NameOfFile = nameOfFile
                }).ToList();
                newDirectory.Files = newFile;
                userDirectory.Append(newDirectory).ToList();
                return 1;
            }
            else
            {
                newDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).LastUpdate = DateTime.Now;
                newDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).OperationType = OperationType.Modify;
                userDirectory?.Append(newDirectory).ToList();
                return 2;
            }
        }

        private static void CreateUserDirectory(ref List<UserDirectory> userDirectory, string dictionary)
        {
            var newDictionary = new UserDirectory() {DirectoryPath = dictionary, Files = new List<Entities.File>()};
            var newListDirectory = userDirectory.Append(newDictionary).ToList();  
            userDirectory = newListDirectory;
        }

        private static int AddFileToDirectory(ref UserDirectory userDirectory, string dictionary, string nameOfFile)
        {
            var cos = UserHaveThisFileInThisDirectory(userDirectory, file: nameOfFile);
            var toSave = userDirectory.Files.Append(new Entities.File() { NameOfFile = nameOfFile }).ToList();
            if (cos is false)
            {
                userDirectory.Files = toSave;
            }
            else
            {
                userDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).OperationType =
                    OperationType.Modify;

                userDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).LastUpdate = DateTime.Now;
            }
            
            return 1;
        }

        private static bool? UserHaveThisFileInThisDirectory(List<UserDirectory> userDirectory,string dictionary, string file)
        {
            var haveThisFile = userDirectory?.FirstOrDefault(d => d.DirectoryPath == dictionary)?.Files.Any(f => f.NameOfFile == file);

            return haveThisFile;
        }

        private static bool? UserHaveThisFileInThisDirectory(UserDirectory userDirectory, string file)
        {
            var haveThisFile = userDirectory?.Files.Any(f => f.NameOfFile == file);

            return haveThisFile;
        }
    }
}
