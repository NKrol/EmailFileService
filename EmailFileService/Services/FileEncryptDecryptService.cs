using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EmailFileService.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Spire.Doc;
using Document = Spire.Doc.Document;
using File = System.IO.File;

namespace EmailFileService.Services
{
    public interface IFileEncryptDecryptService
    {
        void FileEncrypt(string inputFilePath);
        void FileDecrypt(string inputFilePath);
        void FileEncrypt(string email, string inputFilePath);
        void EncryptDoc(string email, string path);
        void EncryptDoc(string path);
        void DecryptDoc(string path);
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

        public void EncryptDoc(string path)
        {
            var document = new Document();
            document.LoadFromFile(path);

            document.Encrypt(GetUserKey());

            //var index = path.LastIndexOf("/", StringComparison.Ordinal);

            //var fileName = path.Substring(index, path.Length - index);

            document.SaveToFile(path, FileFormat.Auto);
        }

        public void DecryptDoc(string path)
        {
            var pathOne = path;//.Replace("_enc.", ".");

            var document = new Document();
            document.LoadFromFile(pathOne, FileFormat.Auto, GetUserKey());

            document.RemoveEncryption();

            //var index = pathOne.LastIndexOf("/", StringComparison.Ordinal);

            //var fileName = pathOne.Substring(index, pathOne.Length - index);

            document.SaveToFile(pathOne.Replace("_enc.", "_dec."), FileFormat.Auto);
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


        /*-------------------------------------------------------------------------------- Method for Test Upload File --------------------------------------------------------------------------------*/
        public void FileEncrypt(string email,string inputFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            var fileExtension = Path.GetExtension(inputFilePath);

            var mainPath = PathEncoder(inputFilePath, fileName);


            var outputFilePath = $"{mainPath}\\{fileName}_enc{fileExtension}";

            this.Encrypt(email, inputFilePath, outputFilePath);

            File.Delete(inputFilePath);
        }

        private void Encrypt(string email, string inputFilePath, string outputFilePath)
        {
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(this.GetUserKey(email), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
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

        public void EncryptDoc(string email, string path)
        {
            var document = new Document();
            document.LoadFromFile(path);

            document.Encrypt(GetUserKey(email));

            //var index = path.LastIndexOf("/", StringComparison.Ordinal);

            //var fileName = path.Substring(index + 1, path.Length - index - 1);

            document.SaveToFile(path, FileFormat.Auto);
        }

        

        private string GetUserKey(string email) // for tests
        {
           var id = _userServiceAccessor.GetId;

            var user = _context.Users
                .Include(u => u.Keys)
                .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            var key = user?.Keys.Key;

            return key;
        }

    }
}
