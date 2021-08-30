using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Entities;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;

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
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        [Route("send")]
        [Authorize]
        public ActionResult SendEmail([FromForm] Email email, [FromForm] IFormFile file)
        {
            var result = _emailService.SendEmail(email, file);


            return Ok(result);
        }

        //Method for test upload File and create account
        /*
        [HttpPost]
        [Route("generateData")]
        [Authorize]
        public ActionResult Generate()
        {
            _emailService.GenerateData();

            return Ok();
        }
        */
    }
}
