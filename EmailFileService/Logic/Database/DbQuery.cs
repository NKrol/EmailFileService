using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Aspose.Words.Drawing;
using AutoMapper;
using EmailFileService.Authorization;
using EmailFileService.Entities;
using EmailFileService.Exception;
using EmailFileService.Model;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EmailFileService.Logic.Database
{
    public interface IDbQuery
    {
        //void AddDirectoryToUser(string email, string userDirectory);
        int AddFilesToDirectory(string directoryName, List<IFormFile> file);
        int AddUserToDb(RegisterUserDto user);
        void AddUserToDb(List<User> user);
        string GetUserKey(int id);
        string GetMainDirectory(string dtoEmail);
        string GenerateClaims(LoginDto dto);
        IEnumerable<ShowMyFilesDto> GetMyFiles(string directory);
        IEnumerable<ShowFolders> GetMyFolders();
        int DeleteFile(string directoryName, string fileName);
        bool UserHaveThisFileInThisDirectory(string directory, string fileName, out string contentType);
        bool UserHaveThisFileInThisDirectory(string directory, string fileName);
        void MoveFile(MoveFileDto dto);
        void TestAddToDirectory(string path);
    }

    public class DbQuery : IDbQuery
    {
        private readonly EmailServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserServiceAccessor _serviceAccessor;
        private readonly IPasswordHasher<User> _hasher;
        private readonly Authentication _authentication;
        private readonly IAuthorizationService _authorizationService;

        private const string ErrorUser = "Can't find user!";
        
        public DbQuery(EmailServiceDbContext dbContext, IMapper mapper ,IUserServiceAccessor serviceAccessor, IPasswordHasher<User> hasher, Authentication authentication, IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _serviceAccessor = serviceAccessor;
            _hasher = hasher;
            _authentication = authentication;
            _authorizationService = authorizationService;
        }
        public void AddDirectoryToUser(string userDirectory)
        {
            var userId = (int)_serviceAccessor.GetId;

            var userWithDirectory = GetUserWithDirectory(userId);
            if (userWithDirectory is null) throw new NotFoundException(ErrorUser);

            //userWithDirectory.Directories ??= new List<UserDirectory>() { new UserDirectory(){DirectoryPath = userDirectory, Children = new List<>(),IsMainDirectory = true,Files = new List<File>()} }.ToList();
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
                    thisFile.OperationType = OperationType.Overwrite;
                    thisFile.LastUpdate = DateTime.Now;
                }
                else
                {
                    var addToDirectory = userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files
                    .Append(new File()
                    {
                        FileSize = d.Length,
                        NameOfFile = d.FileName,
                        CreatedBy = userId
                    }).ToList();
                    userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files = addToDirectory;
                }
            });
            return _dbContext.SaveChanges();
        }
        
        public int AddUserToDb(RegisterUserDto register)
        {
            var userToAdd = _mapper.Map<User>(register);

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

            var user = _dbContext.Users
                .Include(f => f.Directories.Where(d => d.DirectoryPath == directoryName))
                .ThenInclude(e => e.Files.Where(d => d.NameOfFile == fileName)).FirstOrDefault(u => u.Id == userId);

            var fileToRemove = user.Directories.FirstOrDefault(d => d.DirectoryPath == directoryName).Files
                .FirstOrDefault(f => f.NameOfFile == fileName);

            var requirementResult = _authorizationService.AuthorizeAsync(_serviceAccessor.User, fileToRemove,
                new ResourceOperationRequirement(ResourceOperation.Access));

            if (!requirementResult.Result.Succeeded) throw new RequirementException("Not requirement!");

            fileToRemove.Remove();
            return _dbContext.SaveChanges();
        }

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

            var requirementResult = _authorizationService.AuthorizeAsync(_serviceAccessor.User, actualFile,
                new ResourceOperationRequirement(ResourceOperation.Access));

            if (!requirementResult.Result.Succeeded) throw new RequirementException("Not requirement!");

            var directoryToMove = GetUserWithDirectory(userId).Directories
                .FirstOrDefault(ud => ud.DirectoryPath == dto.DirectoryToMove);
            if (directoryToMove is null)
            {
                AddDirectoryToUser(dto.DirectoryToMove);
                directoryToMove = GetUserWithDirectory(userId).Directories
                    .FirstOrDefault(ud => ud.DirectoryPath == dto.DirectoryToMove);
            }

            var enumerable = directoryToMove.Files.Append(new File()
                { NameOfFile = actualFile.NameOfFile, FileSize = actualFile.FileSize, CreatedBy = userId}).ToList();

            actualFile.Move();

            directoryToMove.Files = enumerable;

            _dbContext.SaveChanges();

        }

        public string GetMainDirectory(string dtoEmail)
        {
            var user = _dbContext.Users.Include(f => f.Directories).FirstOrDefault(u => u.Email == dtoEmail);

            var mainDirectoryPath = user?.Directories?.FirstOrDefault(f => f.IsMainDirectory == true)?.DirectoryPath;

            return mainDirectoryPath;
        }

        public string GenerateClaims(LoginDto dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);

            if(user is null) throw new ForbidException("Email or password is wrong!");
            var cos = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (cos == PasswordVerificationResult.Failed) throw new ForbidException("Email or password is wrong!");

            var claims = GenerateClaim(dto.Email,user.Id);

            return claims;
        }


        // -------------------------------------------------------------------- Method to help  --------------------------------------------------------------------//
        
        private User GetUserWithDirectory(int userId) => _dbContext.Users.Include(u => u.Directories).FirstOrDefault(u => u.Id == userId);

        private User GetUserWithDirectoryAndFiles(int userId) => _dbContext.Users.Include(u => u.Directories)
            .ThenInclude(ud => ud.Files).FirstOrDefault(u => u.Id == userId);

        private string GenerateClaim(string email, int id)
        {
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email)
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


        private string MakeDirectory(string path)
        {
            List<string> enumerable = new();
            string pathA = null;
            var counter = 1;
            foreach (var s in path)
            {
                if (s == '/' | counter == path.Length)
                {
                    if (counter == path.Length) pathA += s;
                    enumerable.Add(pathA);
                    pathA = null;
                    counter++;
                }
                else
                {
                    pathA += s;
                    counter++;
                }
            }


            return "";
        }

        // ------------------------------------------------------------ Method to add for parent-child relation userDirectory  ------------------------------------------------------------ //
        /// <summary>
        /// Test method for test new method
        /// </summary>
        /// <param name="path"></param>
        public void TestAddToDirectory(string path)
        {
            var makeUserPathList = MakeUserPathList(path);
            AddDirectoryToUserNew(makeUserPathList);
            DirectoryToString();
        }
        /// <summary>
        /// Generate list of userDirectory with just directoryPath property
        /// </summary>
        /// <param name="path"> String of SendEmailDto.Title </param>
        /// <returns>list of userDirectory with just directoryPath property</returns>
        private List<UserDirectory> MakeUserPathList(string path)
        {
            List<string> enumerable = new();
            string pathA = null;
            var counter = 1;
            foreach (var s in path)
            {
                if (s == '/' | counter == path.Length)
                {
                    if (counter == path.Length) pathA += s;
                    enumerable.Add(pathA);
                    pathA = null;
                    counter++;
                }
                else
                {
                    pathA += s;
                    counter++;
                }
            }

            var listOfDirectory = new List<UserDirectory>();

            enumerable.ForEach(x =>
            {
                var cos = new UserDirectory() { DirectoryPath = x, Children = new List<UserDirectory>() };
                listOfDirectory.Add(cos);
            });
            return listOfDirectory;
        }

        /// <summary>
        /// Create new db row from make sorted list of list from param method
        /// </summary>
        /// <param name="listOfDirectories"></param>
        private void AddDirectoryToUserNew(List<UserDirectory> listOfDirectories)
        {
            var userId = (int)_serviceAccessor.GetId;
            var mainDirectoryUser = _dbContext.Users
                .Include(x => x.Directories)
                .FirstOrDefault(u => u.Id == userId)?
                .Directories
                .FirstOrDefault(f => f.IsMainDirectory == true);
            
            var counter = 0;

            var listOfDirectorySorted = new List<UserDirectory>();

            var any = _dbContext.UserDirectories.Where(f => f.User.Id == userId);

            for (var i = 0; i < listOfDirectories.Count; i++)
            {
                var sameDirectory = any.FirstOrDefault(e => e.DirectoryPath == listOfDirectories[i].DirectoryPath);
                if (sameDirectory is not null)
                {
                    listOfDirectories[i] = sameDirectory;
                }
                else
                {
                    listOfDirectories[i].User = _dbContext.Users.FirstOrDefault(u => u.Id == _serviceAccessor.GetId);
                    listOfDirectories[i].Parent = mainDirectoryUser;
                }
                if ((i + 1) != listOfDirectories.Count)
                {
                    var sameDirectoryChildren = any.FirstOrDefault(e => e.DirectoryPath == listOfDirectories[i + 1].DirectoryPath);
                    listOfDirectories[i].Children.Add(sameDirectoryChildren ?? listOfDirectories[i + 1]);
                }
                if (i > 0)
                {
                    var sameDirectoryParent = any.FirstOrDefault(e => e.DirectoryPath == listOfDirectories[i-1].DirectoryPath);
                    listOfDirectories[i].Parent = sameDirectoryParent ?? listOfDirectories[i - 1];
                }
                listOfDirectorySorted.Add(listOfDirectories[i]);
            }
            
            _dbContext.UserDirectories.UpdateRange(listOfDirectorySorted);

            var cos = _dbContext.SaveChanges();

            Console.WriteLine(cos);

            #region Comment

            /*
            var listOfDirectorySorted = new List<UserDirectory>();

            for (var i = 0; i < listOfDirectories.Count; i++)
            {
                listOfDirectories[i].User = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
                if ((i + 1) != listOfDirectories.Count)
                {
                    listOfDirectories[i].Children.Add(listOfDirectories[i + 1]);
                }
                if (i > 0) listOfDirectories[i].Parent = listOfDirectories[i - 1];
                listOfDirectorySorted.Add(listOfDirectories[i]);
            }

            var userDirectories = _dbContext.Users.Include(d => d.Directories).FirstOrDefault(u => u.Id == userId);
            for (var i = 0; i < listOfDirectorySorted.Count; i++)
            {
                userDirectories = _dbContext.Users.Include(d => d.Directories).FirstOrDefault(u => u.Id == userId);

                var userDirectoriesFirst = _dbContext.Users.Include(d => d.Directories)
                    .ThenInclude(e => e.Children)
                    .FirstOrDefault(u => u.Id == userId);

                var userDirectoriesA = userDirectoriesFirst?.Directories.FirstOrDefault(f => f.DirectoryPath == listOfDirectorySorted[i].DirectoryPath);

                if (userDirectoriesA is not null)
                {
                    var children = userDirectoriesA?.Children.FirstOrDefault(f =>
                        f.DirectoryPath == listOfDirectorySorted[i].DirectoryPath);
                    if (listOfDirectorySorted.Count < i)
                    {
                       children = userDirectoriesA?.Children.FirstOrDefault(f => f.DirectoryPath == listOfDirectorySorted[i+1].DirectoryPath);
                    }
                    if (children is null)
                    {
                        userDirectoriesA?.Children.Add(listOfDirectorySorted[i]);

                        userDirectories?.Directories.Add(userDirectoriesA);

                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        if (i-1 < 0 | i +1 >= listOfDirectorySorted.Count -1 )
                        {
                        }
                        else
                        {
                            listOfDirectorySorted[i + 1].Parent = userDirectoriesFirst?.Directories.FirstOrDefault(f => f.DirectoryPath == listOfDirectorySorted[i-1].DirectoryPath);
                        }
                    }
                }
                else
                {
                    listOfDirectorySorted[i].Parent = mainDirectoryUser;
                    userDirectories?.Directories.Add(listOfDirectorySorted[i]);
                    _dbContext.SaveChanges();
                }
            }

            if (userDirectories != null) _dbContext.Users.Update(userDirectories);
            var saveChanges = _dbContext?.SaveChanges();

            Console.WriteLine(saveChanges.ToString());
            */
            //foreach (var t in listOfDirectories)
            //{
            //    var allDirectoriesUser = _dbContext.UserDirectories.Where(e => e.User.Id == userId).ToList();
            //    t.User = _dbContext.Users.FirstOrDefault(e => e.Id == userId);
            //    var findFirstSameDirectoryWithSameName = _dbContext.UserDirectories.Include(d => d.Children)
            //        .FirstOrDefault(d => d.User.Id == userId & d.DirectoryPath == t.DirectoryPath);
            //    if (findFirstSameDirectoryWithSameName is not null)
            //    {
            //        heleperDirectory = findFirstSameDirectoryWithSameName;
            //    }
            //    else
            //    {
            //        t.Parent = mainDirectoryUser;
            //    }

            //    allDirectoriesUser.Add(t);

            //    var takeChildren = findFirstSameDirectoryWithSameName?.Children.Where(e =>
            //        e.DirectoryPath == t.Children.FirstOrDefault()?.DirectoryPath);


            //    _dbContext.SaveChanges();

            //}

            #endregion
        }
        /// <summary>
        /// Show in Console association of all userDirectory 
        /// </summary>
        public void DirectoryToString()
        {
            var firstOrDefault = _dbContext.UserDirectories.Where(f => f.User.Id == _serviceAccessor.GetId).ToList();

            var cos = "";

            firstOrDefault.ForEach(x =>
            {
               cos += $"Directory path this class: {x.DirectoryPath}, \n" +
                    $"Directory path parent: {x.Parent?.DirectoryPath},\n" +
                    $"Directory path children: {x.Children?.FirstOrDefault()?.DirectoryPath}\n\n\n" +
                    $"" +
                    $"" +
                    $"";
            });

            Console.WriteLine(cos);
        }

        private List<UserDirectory> GetUserDirectories(int id)
        {
            var result = _dbContext.UserDirectories.Where(d => d.User.Id == id).ToList();

            if (result is null) throw new NotFoundException($"Error in {nameof(GetUserDirectories)}");
            
            return result;
        }

        private bool ThisExist(UserDirectory directory)
        {
            var userId = _serviceAccessor.GetId;
            var firstOrDefault = _dbContext?.UserDirectories?.Include(d =>
                    d.Children)
                ?.FirstOrDefault(e => e.User.Id == userId)?
                .Children.FirstOrDefault(c => c.DirectoryPath == directory?.Children?.FirstOrDefault()?.DirectoryPath);
            var result = false || firstOrDefault is not null;

            return result;
        }
        
    }
}
