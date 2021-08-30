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
using Microsoft.Extensions.Logging;
using NLog;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IEmailService
    {
        string SendEmail(Email email, IFormFile file);
        //void GenerateData(); -- test only
    }

    public class EmailService : IEmailService
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IFileEncryptDecryptService _encrypt;
        private readonly IUserServiceAccessor _userServiceAccessor;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailServiceDbContext dbContext, IPasswordHasher<User> hasher,
            IFileEncryptDecryptService encrypt, Authentication authentication, IMapper mapper,
            IUserServiceAccessor userServiceAccessor, ILogger<EmailService> logger)
        {
            _dbContext = dbContext;
            _encrypt = encrypt;
            _userServiceAccessor = userServiceAccessor;
            _logger = logger;
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

                // var nameOfCode = result switch
                // {
                //     1 => "Save to Db was successful",
                //     2 => "File already exist, overwrite it?",
                //     _ => "Something is wrong!"
                // };

                string nameOfCode;

                switch (result)
                {
                    case 2:
                        nameOfCode = "You have This file in this Directory";
                        _logger.LogInformation($"{email.Sender} was overwrite {fileName} in directory: {email.Title} :: {DateTime.Now}");
                        break;
                    case 1:
                        nameOfCode = "Save to Db was successful";
                        _logger.LogInformation($"{email.Sender} was successfully added file: {fileName} to {email.Title} :: {DateTime.Now}");
                        break;
                    default:
                        nameOfCode = "";
                        break;
                }


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
        
        private string GetDirectoryToSaveUsersFiles() => Directory.GetCurrentDirectory() + "/UserDirectory/";
        
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
        /*

        
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

            for (var j = 0; j < 10; j++)
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

                var nameOfCode = "";

                switch (result)
                {
                    case 2:
                        nameOfCode = "You have This file in this Directory";
                        _logger.LogInformation($"{email.Sender} was overwrite {fileName} in directory: {email.Title} :: {DateTime.Now}");
                        break;
                    case 1:
                        nameOfCode = "Save to Db was successful";
                        _logger.LogInformation($"{email.Sender} was successfully added file: {fileName} to {email.Title} :: {DateTime.Now}");
                        break;
                    default:
                        nameOfCode = "";
                        break;
                }

                _encrypt.FileEncrypt(email.Sender, directoryWithUser);
                return nameOfCode;
            }
            else throw new FileNotFoundException("Bad Request");
        }
        */
    }
}
