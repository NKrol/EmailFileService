using EmailFileService.Exception;
using EmailFileService.Model;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using EmailFileService.Entities.Logic;

namespace EmailFileService.Services
{
    public interface IEmailService
    {
        string SendEmail(Email email, List<IFormFile> file);
        //void GenerateData(); // test only
    }

    public class EmailService : IEmailService
    {
        private readonly IDbQuery _dbQuery;
        private readonly IUserServiceAccessor _userServiceAccessor;

        public EmailService(IDbQuery dbQuery, IUserServiceAccessor userServiceAccessor)
        {
            _dbQuery = dbQuery;
            _userServiceAccessor = userServiceAccessor;
        }

        public string SendEmail(Email email, List<IFormFile> file)
        {
            if (file != null && file.Count > 0)
            {
                ValidateTitle(ref email);

                var count = _dbQuery.AddFilesToDirectory(email.Title, file);
                if (count <= 0) throw new NotFoundException("Something is wrong!");

                var filesOperation = new FilesOperation(OperationFile.Add, email.Title, file, _userServiceAccessor, _dbQuery);

                return "asd";
            }
            else throw new FileNotFoundException("Bad Request");
        }

        private static void ValidateTitle(ref Email email)
        {
            var title = email.Title;

            if (title[title.Length - 1] == '/')
            {
                title = title.Remove(title.Length - 1, 1);
            }

            if (title[0] == '/')
            {
                title = title.Remove(0, 1);
            }

            email.Title = title;
        }
    }
}
