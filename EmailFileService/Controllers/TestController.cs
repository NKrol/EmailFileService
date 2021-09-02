using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspose.Words.Drawing;
using EmailFileService.Entities;
using EmailFileService.Entities.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailFileService.Controllers
{
    [ApiController]
    [Route("/api/test")]
    public class TestController : ControllerBase
    {
        private readonly IDbQuery _dbQuery;

        public TestController(IDbQuery dbQuery)
        {
            _dbQuery = dbQuery;
        }


        //[HttpPost]
        //[Route("addUser")]
        //public ActionResult AddUser([FromQuery] string email, [FromQuery]string password)
        //{
        //    var userToAdd = new User()
        //    {
        //        Email = email,
        //        PasswordHash = password
        //    };
            
        //    _dbQuery.AddUserToDb(userToAdd);

        //    return Ok();
        //}

        //[HttpPost]
        //[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        //[Route("addFile")]
        //public ActionResult AddFile([FromForm] List<IFormFile> file)
        //{
        //    var operation = new FilesOperation(OperationFile.Add, "ASD", file);

        //    var operationTest = new FilesOperation(OperationFile.Add, "ASD", file);
        //    var operationTestA = new FilesOperation(OperationFile.Add, "ASD/asd",file);
        //    var operationTestB = new FilesOperation(OperationFile.Add, "ASD/b/b/b",file);
        //    var operationTestC = new FilesOperation(OperationFile.Add, "ASD/c/c/c",file);
        //    var operationTestD = new FilesOperation(OperationFile.Add, "ASD/d/d/d",file);
        //    var operationTestE = new FilesOperation(OperationFile.Add, "ASD/e/e/e", file);
        //    var operationTestF = new FilesOperation(OperationFile.Add, "ASD/f/f/f",file);

        //    return Ok();
        //}

        /*
        [HttpPost]
        [Route("testDb")]
        public ActionResult TryTest()
        {
            //var userToAdd = new User()
            //{
            //    Email = "asd@asd.com",
            //    PasswordHash = "password",
            //    Directories = new List<UserDirectory>(),
            //    Keys = new Keys()
            //    {
            //        Key = "AHSDOAHJSDIASHNDIADJHAID"
            //    }
            //};

            //_dbQuery.AddUserToDb(userToAdd);

            //var userId = _dbQuery.GetUserId("asd@asd.com");

            //_dbQuery.AddDirectoryToUser(userId, new UserDirectory() { DirectoryPath = "test1"});
            //_dbQuery.AddDirectoryToUser(userId, new UserDirectory() { DirectoryPath = "asd_asd_com"});

            //_dbQuery.AddFilesToDirectory(userId, "test1", new File(){NameOfFile = "test.txt"});
            //_dbQuery.AddFilesToDirectory(userId, "test1", new File(){NameOfFile = "test.txt"});
            //_dbQuery.AddFilesToDirectory(userId, "test1", new File(){NameOfFile = "test1.txt"});
            //_dbQuery.AddFilesToDirectory(userId, "test2", new File(){NameOfFile = "test2.txt"});
            var random = new Random();

            var stringsOfDirectoryName = new string[]
            {
                "Files", "Files/Documents", "Files/Documents/Image", "Files/Important/PDF", "Files/Documents/TXT",
                "Files/Garbage", "Files/Garbage1", "Files/Garbage2", "Files/Garbage3", "Files/Garbage4", "Files/Garbage5", "Files/Garbage6", "Files/Garbage7"
            };

            var stringsOfFilesName = new string[] { "test.txt",
                "test1.pdf", 
                "test2.pdf", 
                "test3.pdf", 
                "test4.pdf", 
                "test5.pdf", 
                "test6.pdf", 
                "test7.docx", 
                "test8.txt",
                "test9.docx", 
                "test10.doc", 
                "test11.docx", 
                "test12.docx", 
                "test13.docx", 
                "test14.docx", 
                "test15.doc", 
                "test16.docx",
                "test17.docx", 
                "test18.doc", 
                "test19.docx", 
                "test20.docx", 
                "test21.doc", 
                "test22.docx", 
                "test23.txt", 
                "test24.txt" };

            var listOfUsers = new List<User>();

            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0)
                {
                    listOfUsers.Add(new User()
                    {
                        Email = "test" + i + "@gmail.com",
                        PasswordHash = "password1",
                        Directories = new List<UserDirectory> { new UserDirectory() { DirectoryPath = "Files" } }
                    });
                }
                else
                {
                    listOfUsers.Add(new User()
                    {
                        Email = "test" + i + "@gmail.com",
                        PasswordHash = "password1"
                    });
                }
            }

            listOfUsers.ForEach(u => _dbQuery.AddUserToDb(u));

            //_dbQuery.AddUserToDb(listOfUsers);

            foreach (var listOfUser in listOfUsers)
            {
                var id = _dbQuery.GetUserId(listOfUser.Email);

                for (int i = 0; i < stringsOfDirectoryName.Length; i++)
                {
                    for (int j = 0; j < stringsOfFilesName.Length; j++)
                    {
                        _dbQuery.AddFilesToDirectory(id, stringsOfDirectoryName[random.Next(stringsOfDirectoryName.Length)], new File()
                    {
                        FileSize = random.Next(120, int.MaxValue - 1),
                        NameOfFile = stringsOfFilesName[random.Next(stringsOfFilesName.Length)]
                    });
                    }
                }
            }


            return Ok();
        }
        */
    }
}
