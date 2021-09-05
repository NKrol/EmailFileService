using System.Collections.Generic;
using System.IO;
using EmailFileService.Exception;
using EmailFileService.Logic.Database;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Logic.FileManager
{
    public interface IFilesOperation
    {
        void Action(ServiceFileOperationDto dto);
        MemoryStream DownloadFile(string directory, string fileName);
    }

    public class FilesOperation : IFilesOperation
    {
        private string _directoryName;
        private OperationFile _operationFile;
        private List<IFormFile> _fileForm;
        private readonly IUserServiceAccessor _serviceAccessor;
        private readonly IDbQuery _dbQuery;
        private readonly IFileEncryptDecryptService _encrypt;
        private string _fileName;
        private string _newDirectoryName;
        private string _actualDirectoryName;


        public FilesOperation(IDbQuery dbQuery, IUserServiceAccessor serviceAccessor, IFileEncryptDecryptService encrypt)
        {
            _dbQuery = dbQuery;
            _serviceAccessor = serviceAccessor;
            _encrypt = encrypt;
            _directoryName = Directory.GetCurrentDirectory() + "/UserDirectory/";
        }

        public void Action(ServiceFileOperationDto dto)
        {
            _operationFile = dto.OperationFile;
            _directoryName += _dbQuery.GetMainDirectory(_serviceAccessor?.GetEmail) + "/";
            if (dto.DirectoryName is not null) _directoryName += dto.DirectoryName + "/";
            if(dto.FileName is not null) _fileName = dto.FileName;
            if(dto.FormFiles is not null) _fileForm = dto.FormFiles;
            if(dto.ActualFileDirectory is not null & dto.FileName is not null) _actualDirectoryName =_directoryName + dto.ActualFileDirectory + "/" + dto.FileName.Replace(".", "_enc.");
            if(dto.NewFileDirectory is not null & dto.FileName is not null) _newDirectoryName += _directoryName + dto.NewFileDirectory + "/" + dto.FileName.Replace(".", "_enc.");
            DoAll();
        }

        private void DoAll()
        {
            if (_operationFile == OperationFile.Add)
            {
                AddDirectory(_directoryName);
                var fullPath = _directoryName;
                _fileForm?.ForEach(f =>
                {
                    var pathWithFileName = fullPath + f.FileName;
                    AddFileToDirectory(pathWithFileName, f);
                });
            }
            if (_operationFile == OperationFile.Delete & _fileName is not null & _directoryName is not null)
            {
                DeleteFile(_directoryName + _fileName);
            }

            if (_operationFile == OperationFile.Move & _directoryName is not null & _newDirectoryName is not null)
            {
                MoveFile(_actualDirectoryName, _newDirectoryName);
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

        private void AddFileToDirectory(string fullPath, IFormFile fileStream)
        {
            using var writer = new FileStream(fullPath, FileMode.Create);
            fileStream.CopyTo(writer);
            writer.Close();
            _encrypt.FileEncrypt(fullPath);
        }

        private static void AddDirectory(string pathWithOutFileName)
        {
            var exist = Directory.Exists(pathWithOutFileName);
            if (!exist) Directory.CreateDirectory(pathWithOutFileName);
            else return;
            var existAfter = Directory.Exists(pathWithOutFileName);
            if (!existAfter) throw new NotFoundException("Something is wrong with create directory");
        }

        public MemoryStream DownloadFile(string directory, string fileName)
        {
            var directoryToUse = _directoryName + _dbQuery.GetMainDirectory(_serviceAccessor?.GetEmail) + "/" +
                                 directory + "/" + fileName;
            _encrypt.FileDecrypt(directoryToUse);

            var memory = new MemoryStream();
            using (var reader = new FileStream(directoryToUse, FileMode.Open))
            {
                reader.CopyTo(memory);
                reader.Close();
            }
            _encrypt.FileEncrypt(directoryToUse);
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
