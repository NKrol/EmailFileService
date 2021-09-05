
using EmailFileService.Exception;
using EmailFileService.Logic.Database;
using EmailFileService.Logic.FileManager;
using EmailFileService.Model;

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
        private readonly IFilesOperation _filesOperation;

        public AccountService(IDbQuery dbQuery, IFilesOperation filesOperation)
        {
            _dbQuery = dbQuery;
            _filesOperation = filesOperation;
        }


        public void Register(RegisterUserDto dto)
        {
            var count = _dbQuery.AddUserToDb(dto);
            if (count > 0)
            {
                var mainDirectory = _dbQuery.GetMainDirectory(dto.Email);
                _filesOperation.Action(new ServiceFileOperationDto(){OperationFile = OperationFile.Add, DirectoryName = mainDirectory});
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
