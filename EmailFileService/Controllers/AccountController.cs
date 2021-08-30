using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailFileService.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        [Route("register")]
        public ActionResult Register([FromBody] RegisterUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                throw new CreatedAccountException("");
            }
            _accountService.Register(dto);

            return Ok();
        }

        [HttpPost]
        [Route("login")]
        public ActionResult Login([FromBody] LoginDto dto)
        {
            var token = _accountService.Login(dto);

            return Ok(token);
        }
    }
}
