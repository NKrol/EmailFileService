﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EmailFileService.Controllers
{
    [Route("/api/email")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        [Route("send")]
        [Authorize]
        public ActionResult SendEmail([FromForm]Email email, [FromForm]IFormFile file)
        {
            
            _emailService.SendEmail(email, file);


            return Ok();
        }
        [HttpGet]
        [Route("getMyFiles")]
        [Authorize]
        public ActionResult<IEnumerable<ShowMyFilesDto>> GetMyFiles()
        {
            var myFiles = _emailService.GetMyFiles();

            return Ok(myFiles);
        }

        [HttpPost]
        [Route("register")]
        public ActionResult Register([FromBody] RegisterUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                throw new CreatedAccountException("");
            }
            _emailService.Register(dto);

            return Ok();
        }

        [HttpPost]
        [Route("login")]
        public ActionResult Login([FromBody] LoginDto dto)
        {
            var token = _emailService.Login(dto);

            return Ok(token);
        }

        [HttpGet]
        [Route("downloadFile")]
        public async Task<IActionResult> Download([FromQuery] string fileName)
        {
            var downloadFileDto = _emailService.DownloadFile(fileName);

            var memory = new MemoryStream();
            using (var stream = new FileStream(downloadFileDto.PathToFile, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;

            return File(memory, downloadFileDto.ExtensionFile, fileName);
        }
    }
}