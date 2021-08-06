using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EmailFileService.Entities;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Model.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IEmailService
    {
        void Register(RegisterUserDto dto);
        void SendEmail(Email email, IFormFile file);
        string Login(LoginDto dto);
        IEnumerable<ShowMyFilesDto> GetMyFiles();
        DownloadFileDto DownloadFile(string fileName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IPasswordHasher<User> _hasher;
        private readonly Authentication _authentication;
        private readonly IMapper _mapper;
        private readonly IUserServiceAccessor _userServiceAccessor;

        public EmailService(EmailServiceDbContext dbContext, IPasswordHasher<User> hasher, Authentication authentication, IMapper mapper, IUserServiceAccessor userServiceAccessor)
        {
            _dbContext = dbContext;
            _hasher = hasher;
            _authentication = authentication;
            _mapper = mapper;
            _userServiceAccessor = userServiceAccessor;
        }

        public void Register(RegisterUserDto dto)
        {
            var mainDirectory = GeneratePath(dto.Email);
            var newUser = new User()
            {
                Email = dto.Email,
                Directories = new List<UserDirectory>()
                {
                    new UserDirectory()
                    {
                        DirectoryPath = mainDirectory
                    }
                }
            };
            var passwordHash = _hasher.HashPassword(newUser, dto.Password);

            newUser.PasswordHash = passwordHash;

            var currentDirectory = Directory.GetCurrentDirectory();

            Directory.CreateDirectory($"{GetDirectoryToSaveUsersFiles()}{mainDirectory}");

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

        }

        public void SendEmail(Email email, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var idUser = _userServiceAccessor.GetId;
                var fileExist = UserHaveThisFile(idUser, email.Title + file.FileName);

                if (fileExist) throw new FileExistException("This file exist!");

                var user = _dbContext.Users
                    .Include(d => d.Directories)
                    .ThenInclude(d => d.Files)
                    .AsSplitQuery()
                    .FirstOrDefault(u => u.Email.ToLower() == email.Sender.ToLower());
                if (user is null) throw new NotFoundException("User with this email, wasn't register at page!");

                var isMatch = ReceiverEmailChecker(email.Receiver);
                if (!isMatch) throw new NotFoundException("Receiver email is incorrect!");

                var mainDirectory = _userServiceAccessor.GetMainDirectory;
                var fullPath = $"{GetDirectoryToSaveUsersFiles()}{mainDirectory}/";


                var fileName = file.FileName;

                if (email.Title is not null)
                {
                    Directory.CreateDirectory($"{fullPath}{email.Title}/");
                    fullPath += email.Title + fileName;

                    var userDirectories = user.Directories;

                    var enumerable = userDirectories.ToList();
                    var thisNameDirectory = enumerable.FirstOrDefault(d => d.DirectoryPath == email.Title);
                    if (thisNameDirectory is not null)
                    {
                        var fileOfThisDirectories = thisNameDirectory.Files.ToList();

                        var newDirectoryToUser = thisNameDirectory;

                        if (fileOfThisDirectories.Count > 0)
                        {
                            var cos = fileOfThisDirectories.Append(new Entities.File()
                            {
                                NameOfFile = file.FileName
                            }).ToList();

                            thisNameDirectory.Files = cos;
                            enumerable.Add(thisNameDirectory);
                            user.Directories = enumerable;

                        }
                        else
                        {
                            newDirectoryToUser.Files = new List<Entities.File>()
                            {
                                new Entities.File()
                                {
                                    NameOfFile = file.FileName
                                }
                            };
                            var directories = enumerable.Append(newDirectoryToUser);
                            user.Directories = directories;
                        }
                    }
                    else
                    {
                        var directories = user.Directories.Append(new UserDirectory()
                        {
                            DirectoryPath = email.Title,

                            Files = new List<Entities.File>()
                        {
                            new Entities.File()
                            {
                                NameOfFile = file.FileName
                            }
                        }
                        }).ToList();
                        user.Directories = directories;
                    }

                    _dbContext.SaveChanges();
                }
                else
                {
                    fullPath += fileName;

                }

                using (var writer = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(writer);
                }
            }
            else throw new NotFoundException("Bad Request");
        }

        public string Login(LoginDto dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user is null) throw new ForbidException("Email or password isn't correct!");

            var isCorrect = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (isCorrect == PasswordVerificationResult.Failed) throw new ForbidException("Email or password isn't correct!");

            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("MainDirectory", GeneratePath(dto.Email))
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authentication.JwtIssuer));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
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

            var fullPath = $"{mainDirectory}{userDirectory}/{cos.DirectoryPath}{fileName}";
            

            var ex = Path.GetExtension(fullPath).ToLowerInvariant();

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

        private static bool ReceiverEmailChecker(string email)
        {
            const string receiver = "filesservice@api.com";
            return string.Equals(email, receiver, StringComparison.CurrentCultureIgnoreCase);
        }

        private bool UserHaveThisFile(int? id, string path)
        {
            var userFile = _dbContext.Users
                .Include(u => u.Directories)
                .ThenInclude(ud => ud.Files)
                .Where(u => u.Id == id);
            var mainDirectory = _userServiceAccessor.GetMainDirectory;
            var currentDirectory = Directory.GetCurrentDirectory();

            return File.Exists($"{currentDirectory}/UserDirectory/{mainDirectory}/{path}");
        }

        private bool UserHaveThisDirectory(int? id, string directory)
        {
            var userFile = _dbContext.Users
                .Include(u => u.Directories)
                .Where(u => u.Id == id);
            var mainDirectory = _userServiceAccessor.GetMainDirectory;
            var currentDirectory = Directory.GetCurrentDirectory();

            return Directory.Exists($"{currentDirectory}/UserDirectory/{mainDirectory}");
        }

        private UserDirectory UserHaveThisDirectory(UserDirectory userDirectory)
        {

            return userDirectory;
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
    }
}
