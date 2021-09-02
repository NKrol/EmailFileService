using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EmailFileService.Entities;
using EmailFileService.Entities.Logic;
using EmailFileService.Exception;
using EmailFileService.Model;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EmailFileService.Services
{
    public interface IAccountService
    {
        void Register(RegisterUserDto dto);
        string Login(LoginDto dto);
    }

    public class AccountService : IAccountService
    {
        private readonly IDbQuery _dbQuery;

        public AccountService(IDbQuery dbQuery)
        {
            _dbQuery = dbQuery;
        }


        public void Register(RegisterUserDto dto)
        {
            var count = _dbQuery.AddUserToDb(dto);
            if (count > 0)
            {
                var mainDirectory = _dbQuery.GetMainDirectory(dto.Email);
                var filesOperation = new FilesOperation(OperationFile.Add, mainDirectory);
            }
            else throw new NotFoundException("Bad Access");

        }

        public string Login(LoginDto dto)
        {
            var claims = _dbQuery.GenerateClaims(dto);

            return claims;
        }
    }
}
