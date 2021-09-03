using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Aspose.Words.Drawing;
using EmailFileService.Model;
using EmailFileService.Model.Logic;
using EmailFileService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using File = EmailFileService.Entities.File;

namespace EmailFileService.Controllers
{
    [ApiController]
    [Route("/api/test")]
    public class TestController : ControllerBase
    {
        //private readonly IDbQuery _dbQuery;

       // public TestController(IDbQuery dbQuery)
        //{
          //  _dbQuery = dbQuery;
        //}


        //[HttpPost]
        //[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        //[Route("addFile")]
        //public ActionResult AddFile([FromForm] List<IFormFile> file)
        //{
        //    var operation = new FilesOperation(OperationFile.Add, "ASD", file);

        //    var operationTest = new FilesOperation(OperationFile.Add, "ASD", file);
        //    var operationTestA = new FilesOperation(OperationFile.Add, "ASD/asd", file);
        //    var operationTestB = new FilesOperation(OperationFile.Add, "ASD/b/b/b", file);
        //    var operationTestC = new FilesOperation(OperationFile.Add, "ASD/c/c/c", file);
        //    var operationTestD = new FilesOperation(OperationFile.Add, "ASD/d/d/d", file);
        //    var operationTestE = new FilesOperation(OperationFile.Add, "ASD/e/e/e", file);
        //    var operationTestF = new FilesOperation(OperationFile.Add, "ASD/f/f/f", file);

        //    return Ok();
        //}

        /*
        [HttpPost]
        [Route("testDb")]
        public ActionResult TryTest()
        {
            var userToAdd = new RegisterUserDto()
            {
                Email = "test@test.com",
                Password = "password1",
                ConfirmedPassword = "password1"
            };

            //_dbQuery.AddUserToDb(userToAdd);

            var stringsOfDirectoryName = new string[]
            {
                "Files", "Files/Documents", "Files/Documents/Image", "Files/Important/PDF", "Files/Documents/TXT",
                "Files/Garbage"
            };

            var files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/" + "FileTo");

            var listOfUsers = new List<RegisterUserDto>();

            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0)
                {
                    listOfUsers.Add(new RegisterUserDto()
                    {
                        Email = "test" + i + "@gmail.com",
                        Password = userToAdd.Password,
                        ConfirmedPassword = userToAdd.ConfirmedPassword
                    });
                }
                else
                {
                    listOfUsers.Add(new RegisterUserDto()
                    {
                        Email = "test" + i + "@gmail.com",
                        Password = userToAdd.Password,
                        ConfirmedPassword = userToAdd.ConfirmedPassword
                    });
                }
            }

            listOfUsers.ForEach(u => _dbQuery.AddUserToDb(u));
            var login = new LoginDto();
            Random rand = new Random();
            foreach (var listOfUser in listOfUsers)
            {
                login.Email = listOfUser.Email;
                login.Password = listOfUser.Password;
                var claims = _dbQuery.GenerateClaims(login);
                foreach (var s in stringsOfDirectoryName)
                {
                    var fileList = new List<IFormFile>();
                    for (int i = 0; i < files.Length; i++)
                    {
                        var fileInfo = new FileInfo(files[i]);
                        var fileName = fileInfo.Name;

                        var fileStream = new FileStream(files[i], FileMode.Create);
                        var fileForm = new FormFile(fileStream, Int64.MaxValue, fileStream.Length,
                            stringsOfDirectoryName[i], fileName);
                        fileList.Add(fileForm);
                    }
                    _dbQuery.AddFilesToDirectory(s, fileList);
                    IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();

                    Claim claim = new Claim(ClaimTypes.NameIdentifier, _dbQuery.GetUserId(listOfUser.Email).ToString());
                    Claim claimA = new Claim(ClaimTypes.Email, listOfUser.Email);

                    httpContextAccessor.HttpContext.User.Claims.Append(claim).ToList();
                    httpContextAccessor.HttpContext.User.Claims.Append(claimA).ToList();

                    IUserServiceAccessor accessor = new UserServiceAccessor(httpContextAccessor);

                    new FilesOperation(OperationFile.Add, s, fileList, accessor, _dbQuery);
                }
            }


            return Ok();
        }
        */
    }
}
