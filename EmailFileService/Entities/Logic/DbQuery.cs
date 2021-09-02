using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EmailFileService.Entities.Logic
{
    public interface IDbQuery
    {
        void AddDirectoryToUser(string email, string userDirectory);
        int AddFilesToDirectory(string directoryName, List<IFormFile> file);
        int AddUserToDb(RegisterUserDto user);
        void AddUserToDb(List<User> user);
        int GetUserId(string email);
        string GetUserKey(int id);
        string GetMainDirectory(string dtoEmail);
        string GenerateClaims(LoginDto dto);
        IEnumerable<ShowMyFilesDto> GetMyFiles(string directory);
        IEnumerable<ShowFolders> GetMyFolders();
        int DeleteFile(string directoryName, string fileName);
        bool UserHaveThisFileInThisDirectory(string directory, string fileName, out string contentType);
        bool UserHaveThisFileInThisDirectory(string directory, string fileName);
        void MoveFile(MoveFileDto dto);
    }

    public class DbQuery : IDbQuery
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IUserServiceAccessor _serviceAccessor;
        private readonly IPasswordHasher<User> _hasher;
        private readonly Authentication _authentication;

        private const string ErrorUser = "Can't find user!";
        

        public DbQuery(EmailServiceDbContext dbContext, IUserServiceAccessor serviceAccessor, IPasswordHasher<User> hasher, Authentication authentication)
        {
            _dbContext = dbContext;
            _serviceAccessor = serviceAccessor;
            _hasher = hasher;
            _authentication = authentication;
        }

        public void AddDirectoryToUser(string email, string userDirectory)
        {
            var userId = GetUserId(email);

            var userWithDirectory = GetUserWithDirectory(userId);
            if (userWithDirectory is null) throw new NotFoundException(ErrorUser);

            //userWithDirectory.Directories ??= new List<UserDirectory>() { new UserDirectory(){DirectoryPath = userDirectory, IsMainDirectory = true,Files = new List<File>()} }.ToList();
            var userDirectories = userWithDirectory.Directories.Append(new UserDirectory(){DirectoryPath = userDirectory, Files = new List<File>()}).ToList();

            userWithDirectory.Directories = userDirectories;

            _dbContext.SaveChanges();
        }
        public void AddDirectoryToUser(string userDirectory)
        {
            var userId = (int)_serviceAccessor.GetId;

            var userWithDirectory = GetUserWithDirectory(userId);
            if (userWithDirectory is null) throw new NotFoundException(ErrorUser);

            userWithDirectory.Directories ??= new List<UserDirectory>() { new UserDirectory(){DirectoryPath = userDirectory, IsMainDirectory = true,Files = new List<File>()} }.ToList();
            var userDirectories = userWithDirectory.Directories.Append(new UserDirectory(){DirectoryPath = userDirectory, Files = new List<File>()}).ToList();

            userWithDirectory.Directories = userDirectories;

            _dbContext.SaveChanges();
        }

        public int AddFilesToDirectory(string directoryName, List<IFormFile> file)
        {
            var userId = (int)_serviceAccessor.GetId;
            file.ForEach(d =>
            {
                var userWithDirectoryAndFiles = GetUserWithDirectoryAndFiles(userId);

                if (userWithDirectoryAndFiles is null) throw new NotFoundException(ErrorUser);

                if (userWithDirectoryAndFiles.Directories.Any(ud => ud.DirectoryPath == directoryName) == false)
                {
                    AddDirectoryToUser(directoryName);
                    userWithDirectoryAndFiles = GetUserWithDirectoryAndFiles(userId);
                }

                var thisFile = userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName)
                    .Files
                    .FirstOrDefault(f => f.NameOfFile == d.FileName);
                if (thisFile is not null)
                {
                    thisFile.OperationType = OperationType.Modify;
                    thisFile.LastUpdate = DateTime.Now;
                }
                else
                {
                    var addToDirectory = userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files
                    .Append(new File()
                    {
                        FileSize = d.Length,
                        NameOfFile = d.FileName
                    }).ToList();
                    userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files = addToDirectory;
                }
            });
            return _dbContext.SaveChanges();
        }

        //public string GetKey(int id) => GetUserKey(id);

        public int AddUserToDb(RegisterUserDto register)
        {
            var userToAdd = new User()
            {
                Email = register.Email,
                Directories = new List<UserDirectory>()
                {
                    new UserDirectory()
                    {
                        DirectoryPath = GeneratePath(register.Email),
                        IsMainDirectory = true
                    }
                },
                Keys = new Keys()
                {
                    Key = GenerateKey()
                }
            };

            var hashPassword = _hasher.HashPassword(userToAdd, register.Password);

            userToAdd.PasswordHash = hashPassword;

            _dbContext.Users.Add(userToAdd);
            return _dbContext.SaveChanges();
        }
        public void AddUserToDb(List<User> user)
        {
            _dbContext.Users.AddRange(user);
            _dbContext.SaveChanges();
        }

        public IEnumerable<ShowMyFilesDto> GetMyFiles(string directory)
        {
            var userId = _serviceAccessor.GetId;
            var files = _dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(e => e.Directories.Where(d => d.DirectoryPath == directory))
                .Select(com => new ShowMyFilesDto()
                {
                    UserDirectories = com.DirectoryPath,
                    Files = com.Files.Where(f => f.IsActive == true).Select(c => c.NameOfFile).AsEnumerable()
                });

            if (files is null) throw new System.Exception("You don't have any files");

            var userFilesDto = files;

            return userFilesDto;
        }

        public IEnumerable<ShowFolders> GetMyFolders()
        {
            var userId = _serviceAccessor.GetId;
            var folders = _dbContext.Users.Include(f => f.Directories.Where(d => d.IsMainDirectory == false)).FirstOrDefault(f => f.Id == userId);

            var directories = folders.Directories.Select(f => new ShowFolders() { DirectoryNames = f.DirectoryPath });

            return directories;
        }

        public int DeleteFile(string directoryName, string fileName)
        {
            var userId = (int)_serviceAccessor.GetId;

            var fileToRemove = _dbContext.Users
                .Include(f => f.Directories.Where(d => d.DirectoryPath == directoryName))
                .ThenInclude(e => e.Files.Where(d => d.NameOfFile == fileName)).FirstOrDefault(u => u.Id == userId);
            
            fileToRemove.Directories.FirstOrDefault(d => d.DirectoryPath == directoryName).Files.FirstOrDefault(f => f.NameOfFile == fileName).Remove();

            return _dbContext.SaveChanges();
        }

        public int GetUserId(string email) => _dbContext.Users.FirstOrDefault(u => u.Email == email).Id;

        public string GetUserKey(int id) => _dbContext.Users.Include(u => u.Keys).FirstOrDefault(u => u.Id == id)?.Keys.Key;

        public bool UserHaveThisFileInThisDirectory(string directory, string fileName, out string contentType) // Metoda sprawdza czy taki plik w tym folderze istnieje jeśli tak to zwraca bool i dodatkowo conentType pliku
        {
            if (_serviceAccessor.GetId == null) throw new ForbidException("User not login!");
            var userId = (int)_serviceAccessor.GetId;

            var user = _dbContext.Users.Include(d => d.Directories)
                .ThenInclude(f => f.Files).FirstOrDefault(u => u.Id == userId);

            var check = user.Directories.FirstOrDefault(d => d.DirectoryPath == directory).Files
                .Any(f => f.NameOfFile == fileName);

            var file = user.Directories.FirstOrDefault(d => d.DirectoryPath == directory).Files
                .FirstOrDefault(f => f.NameOfFile == fileName);

            contentType = file?.FileType;

            return check;

        }

        public bool UserHaveThisFileInThisDirectory(string directory, string fileName)
        {
            if (_serviceAccessor.GetId == null) throw new ForbidException("User not login!");
            var userId = (int)_serviceAccessor.GetId;

            var user = _dbContext.Users.Include(d => d.Directories)
                .ThenInclude(f => f.Files).FirstOrDefault(u => u.Id == userId);
            
            var check = user.Directories.FirstOrDefault(d => d.DirectoryPath == directory).Files
                .Any(f => f.NameOfFile == fileName);
            
            return check;

        }

        public void MoveFile(MoveFileDto dto)
        {
            var check = UserHaveThisFileInThisDirectory(dto.ActualDirectory, dto.FileName);
            if (!check) throw new NotFoundException("File doesn't exist!");
            if (_serviceAccessor.GetId == null) throw new ForbidException("User not login!");
            var userId = (int)_serviceAccessor.GetId;
            var files = GetUserWithDirectoryAndFiles(userId);

            var actualFile = files.Directories.FirstOrDefault(d => d.DirectoryPath == dto.ActualDirectory).Files
                .FirstOrDefault(f => f.NameOfFile == dto.FileName);

            var directoryToMove = GetUserWithDirectory(userId).Directories
                .FirstOrDefault(ud => ud.DirectoryPath == dto.DirectoryToMove);
            if (directoryToMove is null)
            {
                AddDirectoryToUser(dto.DirectoryToMove);
                directoryToMove = GetUserWithDirectory(userId).Directories
                    .FirstOrDefault(ud => ud.DirectoryPath == dto.DirectoryToMove);
            }

            var enumerable = directoryToMove.Files.Append(new File()
                { NameOfFile = actualFile.NameOfFile, FileSize = actualFile.FileSize }).ToList();

            actualFile.Remove();

            directoryToMove.Files = enumerable;

            _dbContext.SaveChanges();

        }

        public string GetMainDirectory(string dtoEmail)
        {
            var user = _dbContext.Users.Include(f => f.Directories).FirstOrDefault(u => u.Email == dtoEmail);

            var mainDirectoryPath = user.Directories.FirstOrDefault(f => f.IsMainDirectory == true).DirectoryPath;

            return mainDirectoryPath;
        }

        public string GenerateClaims(LoginDto dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);

            if(user is null) throw new ForbidException("Email or password is wrong!");
            var cos = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (cos == PasswordVerificationResult.Failed) throw new ForbidException("Email or password is wrong!");
            var mainDirectory = GetMainDirectory(dto.Email);

            var claims = GenerateClaim(dto.Email, mainDirectory, user.Id);

            return claims;
        }


        // -------------------------------------------------------------------- Method to help  --------------------------------------------------------------------//





        private User GetUserWithDirectory(int userId) => _dbContext.Users.Include(u => u.Directories).FirstOrDefault(u => u.Id == userId);

        private User GetUserWithDirectoryAndFiles(int userId) => _dbContext.Users.Include(u => u.Directories)
            .ThenInclude(ud => ud.Files).FirstOrDefault(u => u.Id == userId);

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
            if (email.Length > 0) path = email.Replace('@', '_').Replace('.', '_');
            return path;
        }

        private static Random Random = new Random();
        private static string GenerateKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }


    }
}
