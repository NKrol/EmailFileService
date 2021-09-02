using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Exception;
using Microsoft.EntityFrameworkCore;

namespace EmailFileService.Entities.Logic
{
    public interface IDbQuery
    {
        void AddDirectoryToUser(int id, UserDirectory userDirectory);
        void AddFilesToDirectory(int id, string directoryName, File file);
        void AddUserToDb(User user);
        void AddUserToDb(List<User> user);
        int GetUserId(string email);
        string GetUserKey(int id);
    }

    public class DbQuery : IDbQuery
    {
        private readonly EmailServiceDbContext _dbContext;

        private const string ErrorUser = "Can't find user!";

        public DbQuery(EmailServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void AddDirectoryToUser(int id, UserDirectory userDirectory)
        {
            var userWithDirectory = GetUserWithDirectory(id);

            if (userWithDirectory is null) throw new NotFoundException(ErrorUser);

            userWithDirectory.Directories ??= new List<UserDirectory>() { userDirectory };

            var userDirectories = userWithDirectory.Directories.Append(userDirectory).ToList();

            userWithDirectory.Directories = userDirectories;

            _dbContext.SaveChanges();
        }

        public void AddFilesToDirectory(int id, string directoryName, File file)
        {
            var userWithDirectoryAndFiles = GetUserWithDirectoryAndFiles(id);

            if (userWithDirectoryAndFiles is null) throw new NotFoundException(ErrorUser);

            if (userWithDirectoryAndFiles.Directories.Any(ud => ud.DirectoryPath == directoryName) == false)
            {
                AddDirectoryToUser(id, new UserDirectory() { DirectoryPath = directoryName, Files = new List<File>() });
                userWithDirectoryAndFiles = GetUserWithDirectoryAndFiles(id);
            }

            var thisFile = userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName)
                .Files
                .FirstOrDefault(f => f.NameOfFile == file.NameOfFile);
            if (thisFile is not null)
            {
                thisFile.OperationType = OperationType.Modify;
                thisFile.LastUpdate = DateTime.Now;
            }
            else
            {
                var addToDirectory = userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files
                .Append(file).ToList();
                userWithDirectoryAndFiles.Directories.FirstOrDefault(ud => ud.DirectoryPath == directoryName).Files = addToDirectory;
            }
            
            _dbContext.SaveChanges();
        }

        //public string GetKey(int id) => GetUserKey(id);

        public void AddUserToDb(User user)
        {
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }
        public void AddUserToDb(List<User> user)
        {
            _dbContext.Users.AddRange(user);
            _dbContext.SaveChanges();
        }

        public int GetUserId(string email) => _dbContext.Users.FirstOrDefault(u => u.Email == email).Id;

        public string GetUserKey(int id) => _dbContext.Users.Include(u => u.Keys).FirstOrDefault(u => u.Id == id)?.Keys.Key;


        // -------------------------------------------------------------------- Method to Help  --------------------------------------------------------------------//





        private User GetUserWithDirectory(int userId) => _dbContext.Users.Include(u => u.Directories).FirstOrDefault(u => u.Id == userId);

        private User GetUserWithDirectoryAndFiles(int userId) => _dbContext.Users.Include(u => u.Directories)
            .ThenInclude(ud => ud.Files).FirstOrDefault(u => u.Id == userId);


    }
}
