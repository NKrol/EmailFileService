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
using EmailFileService.Exception;
using EmailFileService.Model;
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
        private readonly EmailServiceDbContext _dbContext;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IFileEncryptDecryptService _encrypt;
        private readonly Authentication _authentication;
        private readonly ILogger<EmailService> _logger;

        public AccountService(EmailServiceDbContext dbContext, IPasswordHasher<User> hasher,
            IFileEncryptDecryptService encrypt, Authentication authentication, IMapper mapper,
            IUserServiceAccessor userServiceAccessor, ILogger<EmailService> logger)
        {
            _dbContext = dbContext;
            _hasher = hasher;
            _encrypt = encrypt;
            _authentication = authentication;
            _logger = logger;
        }


        public void Register(RegisterUserDto dto)
        {
            var mainDirectory = GeneratePath(dto.Email);
            var fileKey = GenerateKey();
            var newUser = new User()
            {
                Email = dto.Email,
                Directories = new List<UserDirectory>()
                {
                    new UserDirectory()
                    {
                        DirectoryPath = mainDirectory
                    }
                },
                Keys = new Keys()
                {
                    Key = fileKey
                }
            };
            var passwordHash = _hasher.HashPassword(newUser, dto.Password);

            newUser.PasswordHash = passwordHash;

            Directory.CreateDirectory($"{GetDirectoryToSaveUsersFiles()}{mainDirectory}");

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

            _logger.LogInformation($"{newUser.Email} has been registered, with main directory {mainDirectory}");

        }

        public string Login(LoginDto dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user is null) throw new ForbidException("Email or password isn't correct!");

            var isCorrect = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (isCorrect == PasswordVerificationResult.Failed)
                throw new ForbidException("Email or password isn't correct!");

            var claims = GenerateClaim(user.Email, GeneratePath(dto.Email), user.Id);

            return claims;
        }

        private string GenerateClaim(string email, string userMainPath, int id)
        {
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("MainDirectory", userMainPath)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authentication.JwtIssuer));
            var cred = new SigningCredentials(key,
                SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(_authentication.JwtExpire);

            var token = new JwtSecurityToken(_authentication.JwtIssuer,
                _authentication.JwtIssuer,
                claim,
                expires: expires,
                signingCredentials: cred);

            var tokenHandler = new JwtSecurityTokenHandler();

            return tokenHandler.WriteToken(token);
        }

        private static string GeneratePath(string email)
        {
            var path = "";

            if (email.Length > 0)
            {
                path = email.Replace('@', '_').Replace('.', '_');
            }

            return path;
        }

        private static readonly Random random = new();

        private static string GenerateKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private string GetDirectoryToSaveUsersFiles() => Directory.GetCurrentDirectory() + "/UserDirectory/";
    }
}
