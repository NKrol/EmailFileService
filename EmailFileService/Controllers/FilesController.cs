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
        public ActionResult<IEnumerable<ShowMyFilesDto>> GetMyFiles()
        {
            var myFiles = _fileService.GetMyFiles();

            return Ok(myFiles);
        }

        [HttpGet]
        [Route("download")]
        public async Task<IActionResult> Download([FromQuery] string? directory, [FromQuery] string fileName)
        {
            var downloadFileDto = _fileService.DownloadFileFromDirectory(directory, fileName);

            var memory = new MemoryStream();
            await using (var stream = new FileStream(downloadFileDto.PathToFile, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
                stream.Close();
            }

            //System.IO.File.Delete(downloadFileDto.PathToFile);
            
            memory.Position = 0;

            return File(memory, downloadFileDto.ExtensionFile, fileName);
        }

        [HttpDelete]
        [Route("deleteFile")]
        public ActionResult DeleteFile([FromQuery] string? directory, [FromQuery] string fileName)
        {
            var result = _fileService.DeleteFile(directory, fileName);

            return Ok(result);
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
