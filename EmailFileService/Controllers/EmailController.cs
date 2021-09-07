
using System.Collections.Generic;
using EmailFileService.Logic.Database;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailFileService.Controllers
{
    [Route("/api/email")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IDbQuery _dbQuery;

        public EmailController(IEmailService emailService, IDbQuery dbQuery)
        {
            _emailService = emailService;
            _dbQuery = dbQuery;
        }

        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        [Route("send")]
        [Authorize]
        public ActionResult SendEmail([FromForm] Email email, [FromForm] List<IFormFile> file)
        {
            //var result = _emailService.SendEmail(email, file);
            
            _dbQuery.TestAddToDirectory(email.Title);



            return Ok();
        }

        //Method for test upload File and create account
        
        [HttpPost]
        [Route("generateData")]
        [Authorize]
        public ActionResult Generate()
        {
            //_emailService.GenerateData();

            return Ok();
        }
        
    }
}
