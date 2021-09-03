using System.Collections.Generic;
using System.IO;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Model.Logic;

namespace EmailFileService.Services
{
    public interface IFileService
    {
        (MemoryStream, string) DownloadFileFromDirectory(string? directory, string fileName);
        void DeleteFile(string directory, string fileName);
        IEnumerable<ShowMyFilesDto> GetMyFiles(string directory);
        IEnumerable<ShowFolders> GetFolders();
        void MoveFile(MoveFileDto dto);
    }

    public class FileService : IFileService
    {
        private readonly IFilesOperation _filesOperation;
        private readonly IDbQuery _dbQuery;

        public FileService(IDbQuery dbQuery, IFilesOperation filesOperation)
        {
            _dbQuery = dbQuery;
            _filesOperation = filesOperation;
        }

        public (MemoryStream, string) DownloadFileFromDirectory(string? directory, string fileName)
        {
            var check = _dbQuery.UserHaveThisFileInThisDirectory(directory, fileName, out string contentType);

            if (!check) throw new NotFoundException("We can't find this file!");
            var memory = _filesOperation.DownloadFile(directory, fileName);
            return (memory, contentType);
        }
        
        public void DeleteFile(string directory, string fileName)
        {
            var count = _dbQuery.DeleteFile(directory, fileName);

            if (count <= 0) throw new NotFoundException("This File is already deleted");

            var fileNameToRemove = fileName.Replace(".", "_enc.");
            
            _filesOperation.Action(new ServiceFileOperationDto(){OperationFile = OperationFile.Delete, DirectoryName = directory, FileName = fileNameToRemove});

        }

        public IEnumerable<ShowMyFilesDto> GetMyFiles(string directory)
        {
            var userFilesDto = _dbQuery.GetMyFiles(directory);
            return userFilesDto;
        }

        public IEnumerable<ShowFolders> GetFolders()
        {
            var folders = _dbQuery.GetMyFolders();

            return folders;
        }

        public void MoveFile(MoveFileDto dto)
        {
            ValidateMoveFileDto(ref dto);

            _dbQuery.MoveFile(dto);

            _filesOperation.Action(new ServiceFileOperationDto(){OperationFile = OperationFile.Move,ActualFileDirectory = dto.ActualDirectory, NewFileDirectory = dto.DirectoryToMove, FileName = dto.FileName});
        }

        private static void ValidateMoveFileDto(ref MoveFileDto dto)
        {
            var actualDirectory = dto.ActualDirectory;
            var newDirectory = dto.DirectoryToMove;

            if (actualDirectory[actualDirectory.Length - 1] == '/') actualDirectory = actualDirectory.Remove(actualDirectory.Length - 1, 1);
            if (newDirectory[newDirectory.Length - 1] == '/') newDirectory = newDirectory.Remove(newDirectory.Length - 1, 1);
            if (newDirectory[0] == '/') newDirectory = newDirectory.Remove(0, 1);
            if (actualDirectory[0] == '/') actualDirectory = actualDirectory.Remove(0, 1);
            dto.ActualDirectory = actualDirectory;
            dto.DirectoryToMove = newDirectory;
        }


    }
}
