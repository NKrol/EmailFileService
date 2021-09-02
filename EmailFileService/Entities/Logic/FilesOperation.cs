using System;
using System.Collections.Generic;
using System.IO;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Entities.Logic
{
    public class FilesOperation
    {
        private readonly string Path;

        private readonly string _directoryName;
        private readonly OperationFile _operationFile;
        private readonly List<IFormFile> _fileForm;
        private readonly IUserServiceAccessor _serviceAccessor;
        private readonly IDbQuery _dbQuery;
        private readonly IFileEncryptDecryptService _encrypt;
        private readonly string _fileName;
        private readonly string _newDirectoryName;


        public FilesOperation(IDbQuery dbQuery, IUserServiceAccessor serviceAccessor, string directory, string fileName)
        {
            _dbQuery = dbQuery;
            _serviceAccessor = serviceAccessor;
            Path = Directory.GetCurrentDirectory() + "/UserDirectory/" + _serviceAccessor.GetMainDirectory;
            _directoryName = Path + "/";
            _fileName = fileName;
            if (directory is not null) _directoryName += directory + "/" + fileName;
            _encrypt = new FileEncryptDecryptService(_serviceAccessor, _dbQuery);
        }

        public FilesOperation(OperationFile operationFile, IDbQuery dbQuery, IUserServiceAccessor serviceAccessor, MoveFileDto dto)
        {
            _operationFile = operationFile;
            _dbQuery = dbQuery;
            _serviceAccessor = serviceAccessor;
            Path = Directory.GetCurrentDirectory() + "/UserDirectory/" + _serviceAccessor.GetMainDirectory + "/";
            _fileName = dto.FileName;
            _directoryName = Path + dto.ActualDirectory + "/" + dto.FileName.Replace(".", "_enc.");
            _newDirectoryName = Path + dto.DirectoryToMove + "/" + dto.FileName.Replace(".", "_enc.");
            DoAll();
        }

        public FilesOperation(OperationFile operation, string? directoryName)
        {
            Path = Directory.GetCurrentDirectory() + "/UserDirectory/";
            _operationFile = operation;
            _directoryName = Path;
            if (directoryName is not null) _directoryName += directoryName + "/";
            DoAll();
        }
        public FilesOperation(OperationFile operation, string? directoryName, string fileName, IUserServiceAccessor serviceAccessor)
        {
            Path = Directory.GetCurrentDirectory() + "/UserDirectory/";
            _serviceAccessor = serviceAccessor;
            _operationFile = operation;
            _directoryName = Path + _serviceAccessor.GetMainDirectory + "/";
            _fileName = fileName;
            if (directoryName is not null) _directoryName += directoryName + "/" + fileName;
            else _directoryName += "/" + fileName;
            DoAll();
        }
        public FilesOperation(OperationFile operation, string? directoryName, List<IFormFile> fileStream, IUserServiceAccessor serviceAccessor, IDbQuery dbQuery)
        {
            Path = Directory.GetCurrentDirectory() + "/UserDirectory/";
            _serviceAccessor = serviceAccessor;
            _dbQuery = dbQuery;
            _operationFile = operation;
            _directoryName = Path + _serviceAccessor.GetMainDirectory + "/";
            if (directoryName is not null) _directoryName += directoryName + "/";
            _fileForm = fileStream;
            _encrypt = new FileEncryptDecryptService(_serviceAccessor, _dbQuery);
            DoAll();
        }
        private void DoAll()
        {
            if (_operationFile == OperationFile.Add & _directoryName is not null)
            {
                AddDirectory(_directoryName);
                var fullPath = _directoryName;
                _fileForm?.ForEach(f =>
                {
                    var pathWithFileName = fullPath + f.FileName;
                    AddFileToDirectory(pathWithFileName, f);
                    _encrypt.FileEncrypt(pathWithFileName);
                });
            }

            if (_operationFile == OperationFile.Delete & _fileName is not null & _directoryName is not null)
            {
                DeleteFile(_directoryName);
            }

            if (_operationFile == OperationFile.Move & _directoryName is not null & _newDirectoryName is not null)
            {
                MoveFile(_directoryName, _newDirectoryName);
            }
        }

        private static void MoveFile(string directoryName, string newDirectoryName)
        {
            var exists = Directory.Exists(newDirectoryName);
            if (!exists)
            {
                Directory.CreateDirectory(newDirectoryName.Substring(0, newDirectoryName.LastIndexOf('/')));
            }

            System.IO.File.Copy(directoryName, newDirectoryName);
            DeleteFile(directoryName);
        }

        private static void DeleteFile(string directoryName)
        {
            System.IO.File.Delete(directoryName);
        }

        private static void AddFileToDirectory(string fullPath, IFormFile fileStream)
        {
            using var writer = new FileStream(fullPath, FileMode.Create);
            fileStream.CopyTo(writer);
            writer.Close();
        }

        private static void AddDirectory(string pathWithOutFileName)
        {
            var exist = Directory.Exists(pathWithOutFileName);
            if (!exist) Directory.CreateDirectory(pathWithOutFileName);
            else return;
            var existAfter = Directory.Exists(pathWithOutFileName);
            if (!existAfter) throw new NotFoundException("Something is wrong with create directory");
        }

        public MemoryStream DownloadFile()
        {
            _encrypt.FileDecrypt(_directoryName);

            var memory = new MemoryStream();
            using (var reader = new FileStream(_directoryName, FileMode.Open))
            {
                reader.CopyTo(memory);
                reader.Close();
            }
            _encrypt.FileEncrypt(_directoryName);
            return memory;
        }
    }
    public enum OperationFile
    {
        Add,
        Move,
        Copy,
        Delete,
        Read
    }
}
