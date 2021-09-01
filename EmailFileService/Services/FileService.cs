using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoMapper;
using DocumentFormat.OpenXml.Packaging;
using EmailFileService.Entities;
using EmailFileService.Exception;
using EmailFileService.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IFileService
    {
        DownloadFileDto DownloadFileFromDirectory(string? directory, string fileName);
        string DeleteFile(string? directory, string fileName);
        IEnumerable<ShowMyFilesDto> GetMyFiles();
        void MoveFile(MoveFileDto dto);
    }

    public class FileService : IFileService
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IFileEncryptDecryptService _encrypt;
        private readonly IUserServiceAccessor _userServiceAccessor;
        private readonly ILogger<EmailService> _logger;

        public FileService(EmailServiceDbContext dbContext, IFileEncryptDecryptService encrypt, IUserServiceAccessor userServiceAccessor, ILogger<EmailService> logger)
        {
            _encrypt = encrypt;
            _logger = logger;
            _dbContext = dbContext;
            _userServiceAccessor = userServiceAccessor;
        }

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

        //private MemoryStream GetMemoryStreamOfFileOther(string filePath)
        //{
        //    var memory = new MemoryStream();

        //    using (var stream = new FileStream(filePath, FileMode.Open))
        //    {
        //       stream.CopyToAsync(memory);
        //       stream.Close();
        //    }

        //    return memory;
        //}

        //private MemoryStream GetMemoryStreamOfFile(string fullPath, string fileType, string fileName)
        //{
        //    byte[] byteArray = File.ReadAllBytes(fullPath);
        //    MemoryStream memoryStream = new MemoryStream();

        //    memoryStream.Write(byteArray, 0, byteArray.Length);
        //    using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
        //    {
        //        HtmlConverterSettings settings = new HtmlConverterSettings()
        //        {
        //            PageTitle = "My Page Title"
        //        };
        //        XElement html = HtmlConverter.ConvertToHtml(doc, settings);

        //        var HTMLFilePath = fullPath.Replace(fileName, fileName.Replace(".docx", ".html"));

        //        File.WriteAllText(HTMLFilePath, html.ToStringNewLineOnAttributes());
        //    }

        //    return memoryStream;
        //}

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
                _logger.LogInformation($"{query.Id} delete file: {fileName}, from {directoryToUse}");

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

        public void MoveFile(MoveFileDto dto)
        {
            var actualDirectory = dto.ActualDirectory;
            var userId = _userServiceAccessor.GetId;
            var directoryToMove = dto.DirectoryToMove;
            var fileName = dto.FileName;
            var DirectoryUserFiles = GetDirectoryToSaveUsersFiles() + _userServiceAccessor.GetMainDirectory + "/";
            var query = _dbContext.Users.Include(d => d.Directories)
                .ThenInclude(f => f.Files).FirstOrDefault(u => u.Id == userId);

            if (query is null) throw new NotFoundException("This user don't exist!");

            var haveThisFileInThisDirectory = query.Directories.FirstOrDefault(d => d.DirectoryPath == actualDirectory)
                .Files.Any(f => f.NameOfFile == fileName);

            if (!haveThisFileInThisDirectory) throw new NotFoundException("You don't have this file!");

            var fileNameEnc = fileName.Replace(".", "_enc.");

            actualDirectory = DirectoryUserFiles + actualDirectory + "/" + fileNameEnc;
            directoryToMove = DirectoryUserFiles + directoryToMove + "/";

            var exist = Directory.Exists(directoryToMove);
            var userDirectories = query.Directories.ToList();
            if (!exist)
            {
                Directory.CreateDirectory(directoryToMove);
                var cos = userDirectories.Append(new UserDirectory()
                    { DirectoryPath = dto.DirectoryToMove}).ToList();
                query.Directories = cos;
                _dbContext.SaveChanges();
                var userasd = _dbContext.Users.Include(d => d.Directories)
                    .ThenInclude(f => f.Files).FirstOrDefault(u => u.Id == userId);
                userDirectories = userasd?.Directories.ToList();
            }

            //var updateDirectories = query.Directories.ToList();
            var actualDirectoryA = userDirectories.FirstOrDefault(ud => ud.DirectoryPath == dto.ActualDirectory);
            var directoryToMeveFile = userDirectories.FirstOrDefault(ud => ud.DirectoryPath == dto.DirectoryToMove);
            var file = userDirectories.FirstOrDefault(ud => ud.DirectoryPath == dto.ActualDirectory).Files
                .FirstOrDefault(f => f.NameOfFile == fileName);
            var newFile = new Entities.File()
            {
                FileSize = file.FileSize, FileType = file.FileType,
                NameOfFile = file.NameOfFile, OperationType = OperationType.Create
            };
            var fileSecond = directoryToMeveFile.Files.Append(newFile).ToList();
            directoryToMeveFile.Files = fileSecond;
            file.OperationType = OperationType.Delete;
            file.LastUpdate = DateTime.Now;
            file.IsActive = false;
            //actualDirectoryA.Files.Append(file).ToList();

            _dbContext.SaveChanges();

            directoryToMove += fileNameEnc;

            File.Copy(actualDirectory, directoryToMove, true);
            //File.Delete(actualDirectory);
            
            //_dbContext.SaveChangesAsync();
        }
        
        private static string GeneratePathToDeleteFile(string path)
        {
            var fileNameWithoutEx = Path.GetFileNameWithoutExtension(path);

            var ex = Path.GetExtension(path);

            var fileNameToDelete = path.Replace(fileNameWithoutEx + ex, fileNameWithoutEx + "_enc" + ex);

            return fileNameToDelete;
        }

        private string GetDirectoryToSaveUsersFiles() => Directory.GetCurrentDirectory() + "/UserDirectory/";
        
    }
}
