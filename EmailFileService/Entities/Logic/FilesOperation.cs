using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using EmailFileService.Exception;
using EmailFileService.Services;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Entities.Logic
{
    public class FilesOperation
    {
        private const string Path = "C:/Users/Bercik/source/repos/EmailFileService/EmailFileService/UserDirectory/";

        private readonly string _directoryName;
        private readonly OperationFile _operationFile;
        private readonly List<IFormFile> _fileForm;
        private readonly IFileEncryptDecryptService _encrypt;

        public FilesOperation(OperationFile operation, string? directoryName)
        {
            _operationFile = operation;
            _directoryName = Path;
            if (directoryName is not null) _directoryName += directoryName + "/";
            _encrypt = new FileEncryptDecryptService();
            DoAll();
        }
        public FilesOperation(OperationFile operation, string? directoryName, List<IFormFile> fileStream)
        {
            _operationFile = operation;
            _directoryName = Path;
            if (directoryName is not null) _directoryName += directoryName + "/";
            _fileForm = fileStream;
            _encrypt = new FileEncryptDecryptService();
            DoAll();
        }
        private void DoAll()
        {
            //var idString = new HttpContextAccessor().HttpContext.User.Claims
            //    .FirstOrDefault(f => f.Type == ClaimTypes.NameIdentifier).Value.ToString();
            //var id = int.Parse(idString);
            
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
        }

        private void AddFileToDirectory(string fullPath, IFormFile fileStream)
        {
            using var writer = new FileStream(fullPath, FileMode.Create);
            fileStream.CopyTo(writer);
            writer.Close();
        }

        private void AddDirectory(string pathWithOutFileName)
        {
            var exist = Directory.Exists(pathWithOutFileName);
            if (!exist) Directory.CreateDirectory(pathWithOutFileName);
            else return;
            var existAfter = Directory.Exists(pathWithOutFileName);
            if (!existAfter) throw new NotFoundException("Something is wrong with create directory");
        }
    }

    public enum OperationFile
    {
        Add,
        Move,
        Copy
    }
}
