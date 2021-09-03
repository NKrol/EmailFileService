using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailFileService.Controllers
{
    [ApiController]
    [Route("api/file")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        [Route("myFiles")]
        public ActionResult<IEnumerable<ShowMyFilesDto>> GetMyFiles(string directory)
        {
            var myFiles = _fileService.GetMyFiles(directory);

            return Ok(myFiles);
        }

        [HttpGet]
        [Route("myFolders")]
        public ActionResult<IEnumerable<ShowFolders>> GetFolders()
        {
            var folders = _fileService.GetFolders();

            return Ok(folders);
        }

        [HttpGet]
        [Route("download")]
        public FileStreamResult Download([FromQuery] string? directory, [FromQuery] string fileName)
        {
            var (memoryStream, contentType) = _fileService.DownloadFileFromDirectory(directory, fileName);

            memoryStream.Position = 0;

            return File(memoryStream, contentType, fileName);
        }

        [HttpDelete]
        [Route("deleteFile")]
        public ActionResult DeleteFile([FromQuery] string directory, [FromQuery]string fileName)
        {
            _fileService.DeleteFile(directory, fileName);

            return Ok();
        }

        [HttpPut]
        [Route("moveFile")]
        public ActionResult MoveFile([FromBody] MoveFileDto dto)
        {
            _fileService.MoveFile(dto);

            return Ok();
        }
    }
}
