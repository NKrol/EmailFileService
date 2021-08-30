using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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
                .ThenInclude(f => f.Files).Single(u => u.Id == userId);

            if (query is null) throw new NotFoundException("This user don't exist!");

            var haveThisFileInThisDirectory = query.Directories.FirstOrDefault(d => d.DirectoryPath == actualDirectory)
                .Files.Any(f => f.NameOfFile == fileName);

            if (!haveThisFileInThisDirectory) throw new NotFoundException("You don't have this file!");

            var fileNameEnc = fileName.Replace(".", "_enc.");

            actualDirectory = DirectoryUserFiles + actualDirectory + "/" + fileNameEnc;
            directoryToMove = DirectoryUserFiles + directoryToMove + "/";

            var exist = Directory.Exists(directoryToMove);
            if (!exist)
            {
                Directory.CreateDirectory(directoryToMove);
                var userDirectories = query.Directories.Append(new UserDirectory()
                    { DirectoryPath = dto.DirectoryToMove, Files = new List<Entities.File>() }).ToList();
                query.Directories = userDirectories;
            }

            directoryToMove += fileNameEnc;

            File.Copy(actualDirectory, directoryToMove, true);
            File.Delete(actualDirectory);

            MoveFileDb(dto, ref query);

            _dbContext.SaveChangesAsync();
        }

        private static string GeneratePathToDeleteFile(string path)
        {
            var fileNameWithoutEx = Path.GetFileNameWithoutExtension(path);

            var ex = Path.GetExtension(path);

            var fileNameToDelete = path.Replace(fileNameWithoutEx + ex, fileNameWithoutEx + "_enc" + ex);

            return fileNameToDelete;
        }

        private string GetDirectoryToSaveUsersFiles() => Directory.GetCurrentDirectory() + "/UserDirectory/";

        private void MoveFileDb(MoveFileDto dto, ref User user)
        {
            var thisFile = user.Directories.FirstOrDefault(d => d.DirectoryPath == dto.ActualDirectory).Files
                .FirstOrDefault(f => f.NameOfFile == dto.FileName);
            user.Directories.FirstOrDefault(d => d.DirectoryPath == dto.ActualDirectory).Files
                .FirstOrDefault(f => f.NameOfFile == dto.FileName).Remove();

            var files = user.Directories.FirstOrDefault(d => d.DirectoryPath == dto.DirectoryToMove).Files
                .Append(thisFile).ToList();
            user.Directories.FirstOrDefault(d => d.DirectoryPath == dto.DirectoryToMove).Files = files;
        }
    }
}
