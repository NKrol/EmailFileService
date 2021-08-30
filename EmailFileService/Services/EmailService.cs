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
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
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
        DownloadFileDto DownloadFileFromDirectory(string? directory, string fileName);
        string DeleteFile(string? directory, string fileName);
        void GenerateData();
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
                ValidateTitle(ref email);


                var mainDirectory = _userServiceAccessor.GetMainDirectory;
                var directoryWithUser = $"{GetDirectoryToSaveUsersFiles()}{mainDirectory}/";

                var fileName = file.FileName;
                var contentType = file.ContentType;
                var fileSize = file.Length;
                int result;
                if (email.Title is not null)
                {
                    result = AddFileToDb(email.Sender, email.Title, fileName, contentType, fileSize, out var fullPath);
                    directoryWithUser += fullPath;
                }
                else
                {
                    result = AddFileToDb(email.Sender, "", fileName, contentType, fileSize, out var fullPath);
                    directoryWithUser += fullPath;
                }

                Directory.CreateDirectory(directoryWithUser.Replace("/" + fileName, ""));

                using (var writer = new FileStream(directoryWithUser, FileMode.Create))
                {
                    file.CopyTo(writer);
                    writer.Close();
                }

                var nameOfCode = result switch
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

        private static void ValidateTitle(ref Email email)
        {
            var title = email.Title;

            if (title[title.Length - 1] == '/')
            {
                title = title.Remove(title.Length - 1, 1);
            }

            if (title[0] == '/')
            {
                title = title.Remove(0, 1);
            }

            email.Title = title;
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
        /*
        public DownloadFileDto DownloadFile(string fileName)
        {
            var mainDirectory = GetDirectoryToSaveUsersFiles();
            var userDirectory = _userServiceAccessor.GetMainDirectory;
            var userId = _userServiceAccessor.GetId;
            var user = _dbContext.Users
                .Include(c => c.Directories)
                .ThenInclude(c => c.Files)
                .Single(d => d.Id == userId);

            if (user is null) throw new NotFoundException("");

           var userDirectories = user.Directories.ToList();
            var userFile = userDirectories.SelectMany(f => f.Files).FirstOrDefault(f => f.NameOfFile == fileName);

            var cos = userDirectories.FirstOrDefault(f => f.Files.Contains(userFile));

            if (userFile is null) throw new NotFoundException("Login user don't have this file!");

            var fullPath = $"{mainDirectory}{userDirectory}/{cos.DirectoryPath}/{fileName}";

            //var ex = Path.GetExtension(fullPath).ToLowerInvariant();

            _encrypt.FileDecrypt(fullPath);

            return new DownloadFileDto()
            {
                ExtensionFile = userFile.FileType,
                PathToFile = fullPath
            };
        }
        */
        public DownloadFileDto DownloadFileFromDirectory(string? directory, string fileName)
        {
            var userFiles = GetDirectoryToSaveUsersFiles();
            var mainDirectoryUser = _userServiceAccessor.GetMainDirectory;
            var userId = _userServiceAccessor.GetId;
            var forMomentString = directory;
            if (string.IsNullOrEmpty(directory))
            {
                forMomentString = mainDirectoryUser;
            }

            var query = _dbContext.FindUser(userId, forMomentString, fileName);

            if (query is null) throw new NotFoundException("User don't have file in this direction");

            var fullPath = userFiles + mainDirectoryUser + "/" + directory + "/" + fileName;

            _encrypt.FileDecrypt(fullPath);

            return new DownloadFileDto()
            {
                ExtensionFile = query.Directories.Single(d => d.DirectoryPath == forMomentString).Files.Single(f => f.NameOfFile == fileName).FileType,
                PathToFile = fullPath
            };
        }

        public string DeleteFile(string? directory, string fileName)
        {
            string result = null;
            var directoryToUse = directory;
            var userId = _userServiceAccessor.GetId;
            var fullPath = GetDirectoryToSaveUsersFiles();
            if (directory is null)
            {
                directoryToUse = _userServiceAccessor.GetMainDirectory;
                fullPath += directoryToUse + "/" + fileName;
            }
            else
            {
                fullPath += _userServiceAccessor.GetMainDirectory + "/" + directoryToUse + "/" + fileName;
            }

            var query = _dbContext.FindUser(userId, directoryToUse, fileName);

            if (query is null) throw new NotFoundException("User don't have this file");

            try
            {
                var fileToDelete = query.Directories.Single(d => d.DirectoryPath == directoryToUse).Files
                                .Single(f => f.NameOfFile == fileName);
                fileToDelete.Remove();
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("This file didn't exist or was deleted before!");
            }

            var fileNameToDelete = GeneratePathToDeleteFile(fullPath);

            File.Delete(fileNameToDelete);

            result = $"User of id: {userId}, was delete file: {fileName}, in {directoryToUse} directory";

            _dbContext.SaveChanges();

            return result;
        }
        
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

        private static string GeneratePathToDeleteFile(string path)
        {
            var fileNameWithoutEx = Path.GetFileNameWithoutExtension(path);

            var ex = Path.GetExtension(path);

            var fileNameToDelete = path.Replace(fileNameWithoutEx + ex, fileNameWithoutEx + "_enc" + ex);

            return fileNameToDelete;
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

        private static readonly Random random = new();

        private static string GenerateKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private int AddFileToDb(string email, string? dictionaryName, string fileName, string contentType, long fileSize, out string fullPath)
        {
            var user = _dbContext.Users
                .Include(u => u.Directories)
                .ThenInclude(ud => ud.Files)
                .AsSplitQuery()
                .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (user is null) throw new NotFoundException("We don't have this email");

            var dictionary = user?.Directories.ToList();

            if (dictionaryName?.Length > 0)
            {
                var result = AddFileToDirectory(ref dictionary, dictionaryName, fileName, contentType, fileSize);
                if (result < 0)
                {
                    fullPath = $"{dictionaryName}/{fileName}{contentType}";
                    return result;
                }
                fullPath = $"{dictionaryName}/{fileName}";
                user.Directories = dictionary;
                _dbContext.SaveChanges();
                return result;
            }
            else
            {
                var mainDirectory = dictionary.FirstOrDefault(d => d.DirectoryPath == _userServiceAccessor.GetMainDirectory);
                var result = AddFileToDirectory(ref mainDirectory, fileName, contentType, fileSize);
                if (result < 0)
                {
                    fullPath = $"{dictionaryName}/{fileName}{contentType}";
                    return result;
                }
                user.Directories = dictionary;
                fullPath = $"/{fileName}";
                _dbContext.SaveChanges();
                return result;
            }
        }

        private static int AddFileToDirectory(ref List<UserDirectory> userDirectory, string dictionary, string nameOfFile, string contentType, long fileSize) // Dodawanie do nie istniejącego UserDirectory
        {
            var checkIfThisFileExist = UserHaveThisFileInThisDirectory(userDirectory, dictionary, file: nameOfFile, contentType);
            var thisDictionary = userDirectory.FirstOrDefault(d => d.DirectoryPath == dictionary);
            if (thisDictionary is null)
            {
                CreateUserDirectory(ref userDirectory, dictionary);
            }
            var newDirectory = userDirectory.FirstOrDefault(d => d.DirectoryPath == dictionary);
            if (checkIfThisFileExist is true)
            {
                newDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).LastUpdate = DateTime.Now;
                newDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).OperationType =
                    OperationType.Modify;
                userDirectory?.Append(newDirectory).ToList();


                return 2;
            }
            else
            {
                var newFile = newDirectory?.Files.Append(new Entities.File()
                {
                    NameOfFile = nameOfFile,
                    FileSize = fileSize,
                    FileType = contentType
                }).ToList();
                newDirectory.Files = newFile;
                userDirectory.Append(newDirectory).ToList();
                return 1;
            }
        }

        private static void CreateUserDirectory(ref List<UserDirectory> userDirectory, string dictionary)
        {
            var newDictionary = new UserDirectory() { DirectoryPath = dictionary, Files = new List<Entities.File>() };
            var newListDirectory = userDirectory.Append(newDictionary).ToList();
            userDirectory = newListDirectory;
        }

        private static int AddFileToDirectory(ref UserDirectory userDirectory, string nameOfFile, string contentType, long fileSize)
        {
            var cos = UserHaveThisFileInThisDirectory(userDirectory, file: nameOfFile, contentType);

            if (cos is false)
            {
                var toSave = userDirectory.Files
                            .Append(new Entities.File() { NameOfFile = nameOfFile, FileType = contentType, FileSize = fileSize })
                            .ToList();

                userDirectory.Files = toSave;
            }
            else
            {
                userDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).FileSize = fileSize;

                userDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).OperationType =
                    OperationType.Modify;

                userDirectory.Files.FirstOrDefault(f => f.NameOfFile == nameOfFile).LastUpdate = DateTime.Now;
            }

            return 1;
        }

        private static bool? UserHaveThisFileInThisDirectory(List<UserDirectory> userDirectory, string dictionary, string file, string contentType)
        {
            var haveThisFile = userDirectory?.FirstOrDefault(d => d.DirectoryPath == dictionary)?.Files.Any(f => f.NameOfFile == file & f.FileType == contentType);

            return haveThisFile;
        }

        private static bool? UserHaveThisFileInThisDirectory(UserDirectory userDirectory, string file, string contentType)
        {
            var haveThisFile = userDirectory?.Files.Any(f => f.NameOfFile == file & f.FileType == contentType);

            return haveThisFile;
        }

        /*---------------------------------------------- Method for Test Upload File --------------------------------------------------------------------------------*/
        public void GenerateData()
        {
            var titleTab = new string[] { "Files", "Files/Documents", "Files/Documents/Text", "Files/Documents/Docs", "Files/Documents/Pdf", "Files/Documents/Jpg" };

            var email = new Email() { Receiver = "filesservice@api.com", Sender = "nkrol@gmail.com" };
            var path = Directory.GetCurrentDirectory() + "/FileTo/";

            var cos = Directory.GetFiles(path).Length;

            var files = GetAllFiles(path);
            var count = 0;
            if (cos < 10)
            {
                for (var i = 0; i < 5; i++)
                {
                    foreach (var file in files)
                    {
                        File.Copy(path + file.Key + file.Value, path + file.Key + count + i + file.Value);
                    }
                }
            }

            var userCount = _dbContext.Users.Count();
            if (userCount > 0)
            {
                return;
            }
            var listUser = new List<string>();

            var registerUser = new RegisterUserDto() { Email = "asd@asd.com", Password = "password1", ConfirmedPassword = "password1" };

            for (var j = 0; j < 30; j++)
            {
                registerUser.Email = "nkrol" + j + "@gmail.com";

                Register(registerUser);

                listUser.Add(registerUser.Email);
            }

            var filesAfterCopy = GetAllFiles(path);

            for (var i = 0; i < listUser.Count; i++)
            {
                string result;
                email.Sender = listUser[i];


                //result = SendEmail(email, path + "notatnik" + h + ".txt", "notatnik" + h + ".txt");
                //Console.WriteLine(result);
                //Console.WriteLine($"{email.Sender} add file: notatnik" + h + $".txt to {email.Title}" );
                foreach (var file in filesAfterCopy)
                {
                    var extension = file.Value.Replace(".", "").ToUpper();
                    email.Title = "Files/Documents/" + extension;
                    var sendEmail = SendEmail(email, path + file.Key + file.Value, file.Key + file.Value, file.Value);
                    Console.WriteLine(sendEmail);
                    Console.WriteLine($"{email.Sender} add file: {file.Key}{file.Value} to {email.Title}");
                }
            }
        }

        private Dictionary<string, string> GetAllFiles(string path)
        {
            var files = Directory.GetFiles(path);
            var allFilesWithExtension = new Dictionary<string, string>();

            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file);
                allFilesWithExtension.Add(fileNameWithoutExtension, extension);
            }
            return allFilesWithExtension;
        }

        public string SendEmail(Email email, string file, string fileName, string fileExtension)
        {
            if (file != null && file.Length > 0)
            {
                ValidateTitle(ref email);



                var mainDirectory = GeneratePath(email.Sender);
                var directoryWithUser = $"{GetDirectoryToSaveUsersFiles()}{mainDirectory}/";

                var contentType = GetTypeOfFile()[fileExtension];
                var fileSize = file.Length;
                int result;
                if (email.Title is not null)
                {
                    result = AddFileToDb(email.Sender, email.Title, fileName, contentType, fileSize, out var fullPath);
                    directoryWithUser += fullPath;
                }
                else
                {
                    result = AddFileToDb(email.Sender, "", fileName, contentType, fileSize, out var fullPath);
                    directoryWithUser += fullPath;
                }

                Directory.CreateDirectory(directoryWithUser.Replace("/" + fileName, ""));

                File.Copy(file, directoryWithUser);

                var nameOfCode = result switch
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
    }
}
