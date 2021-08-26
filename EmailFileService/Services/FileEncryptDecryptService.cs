using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EmailFileService.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IFileEncryptDecryptService
    {
        void FileEncrypt(string inputFilePath);
        void FileDecrypt(string inputFilePath);
    }

    public class FileEncryptDecryptService : IFileEncryptDecryptService
    {
        private readonly IUserServiceAccessor _userServiceAccessor;
        private readonly EmailServiceDbContext _context;

        public FileEncryptDecryptService(IUserServiceAccessor userServiceAccessor, EmailServiceDbContext context)
        {
            _userServiceAccessor = userServiceAccessor;
            _context = context;
        }

        public void FileEncrypt(string inputFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            var fileExtension = Path.GetExtension(inputFilePath);

            var mainPath = PathEncoder(inputFilePath, fileName);
            

            var outputFilePath = $"{mainPath}\\{fileName}_enc{fileExtension}";
            
            this.Encrypt(inputFilePath, outputFilePath);

            File.Delete(inputFilePath);

        }
        public void FileDecrypt(string inputFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            var fileExtension = Path.GetExtension(inputFilePath);

            var mainPath = PathEncoder(inputFilePath, fileName);

            var inPutFileName = $"{mainPath}\\{fileName}_enc{fileExtension}";

            var fileDec = fileName.Replace("_enc", "");

            var outPutFileName = $"{mainPath}\\{fileDec}{fileExtension}";

            this.Decrypt(inPutFileName, outPutFileName);
        }

        private string PathEncoder(string path, string fileName)
        {
            var nextToUserDirectory = path.IndexOf(fileName, StringComparison.Ordinal);

            var takeDirectory = path.Substring(0 ,nextToUserDirectory);
            
            return takeDirectory;
        }


        private void Encrypt(string inputFilePath, string outputFilePath)
        {
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(this.GetUserKey(), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var fsOutput = new FileStream(outputFilePath, FileMode.Create))
                {
                    using (var cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (var fsInput = new FileStream(inputFilePath, FileMode.Open))
                        {
                            int data;
                            while ((data = fsInput.ReadByte()) != -1)
                            {
                                cs.WriteByte((byte)data);
                            }
                        }
                    }
                }
            }
        }

        private void Decrypt(string inputFilePath, string outputFilePath)
        {
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(this.GetUserKey(), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var fsInput = new FileStream(inputFilePath, FileMode.Open))
                {
                    using (var cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var fsOutput = new FileStream(outputFilePath, FileMode.Create))
                        {
                            int data;
                            while ((data = cs.ReadByte()) != -1)
                            {
                                fsOutput.WriteByte((byte)data);
                            }
                        }
                    }
                }
            }
        }

        private string GetUserKey()
        {
            var id = _userServiceAccessor.GetId;

            var user = _context.Users
                .Include(u => u.Keys)
                .FirstOrDefault(u => u.Id == id);

            var key = user?.Keys.Key;

            return key;

        }

    }
}
