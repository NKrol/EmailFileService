using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Web;
using System.Threading.Tasks;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Spire.Doc;

namespace EmailFileService.Controllers
{
    [ApiController]
    [Route("api/file")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IFileEncryptDecryptService _encrypt;

        public FilesController(IFileService fileService, IFileEncryptDecryptService encrypt)
        {
            _fileService = fileService;
            _encrypt = encrypt;
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

            //await using (var stream = new FileStream(downloadFileDto.PathToFile, FileMode.Open))
            //{
            //    await stream.CopyToAsync(memory);
            //    stream.Close();

            //}

            // KURWA JAK

            var document = new Document();
            document.LoadFromFile(downloadFileDto.PathToFile, FileFormat.Doc);
            document.SaveToStream(memory, FileFormat.Doc);

            var file = new FileStreamResult(memory, downloadFileDto.ExtensionFile);

            //System.IO.File.Delete(downloadFileDto.PathToFile);
            memory.Position = 0;
            //return File(memory, downloadFileDto.ExtensionFile, fileName);
            return file;
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
