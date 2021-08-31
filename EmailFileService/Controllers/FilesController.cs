using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Web;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Loading;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Spire.Doc;
using Spire.Doc.Documents;
using Document = Spire.Doc.Document;

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
            //   await stream.CopyToAsync(memory);
              //  stream.Close();

            //}

            //KURWA JAK
            var document = new Aspose.Words.Document(downloadFileDto.PathToFile, new LoadOptions("X2B5RXTRFI9OGFQP5NGWI3G2EZ3ALYXG"));
            var pathToHtml = downloadFileDto.PathToFile.Replace(fileName, fileName.Replace(".doc", ".html"));
            document.Save(pathToHtml, SaveFormat.Html);
            
            memory.Close();

            var memoryNew = new MemoryStream();

            await using (var stream = new FileStream(pathToHtml, FileMode.Open))
            {
                await stream.CopyToAsync(memoryNew);
                stream.Close();
            }

            var html = new FileExtensionContentTypeProvider().TryGetContentType(
                downloadFileDto.PathToFile.Replace(fileName, fileName.Replace(".doc", ".html")),
                out string contentType);
            var file = new FileStreamResult(memoryNew, contentType);

            //System.IO.File.Delete(downloadFileDto.PathToFile);
            memoryNew.Position = 0;
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
