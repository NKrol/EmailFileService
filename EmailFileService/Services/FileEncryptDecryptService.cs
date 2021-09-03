using System;
using System.IO;
using System.Security.Cryptography;
using EmailFileService.Model.Logic;
using Spire.Doc;
using Document = Spire.Doc.Document;
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
        private readonly IDbQuery _dbQuery;

        public FileEncryptDecryptService(IUserServiceAccessor userServiceAccessor, IDbQuery dbQuery)
        {
            _userServiceAccessor = userServiceAccessor;
            _dbQuery = dbQuery;
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

        private static string PathEncoder(string path, string fileName)
        {
            var nextToUserDirectory = path.IndexOf(fileName, StringComparison.Ordinal);

            var takeDirectory = path.Substring(0 ,nextToUserDirectory);
            
            return takeDirectory;
        }

        public void EncryptDoc(string path)
        {
            var document = new Document();
            document.LoadFromFile(path);

            //document.Encrypt();

            //var index = path.LastIndexOf("/", StringComparison.Ordinal);

            //var fileName = path.Substring(index, path.Length - index);

            document.SaveToFile(path, FileFormat.Auto);
        }

        public void DecryptDoc(string path)
        {
            var pathOne = path;//.Replace("_enc.", ".");

            var document = new Document();
            //document.LoadFromFile(pathOne, FileFormat.Auto, KeyA);

            document.RemoveEncryption();

            //var index = pathOne.LastIndexOf("/", StringComparison.Ordinal);

            //var fileName = pathOne.Substring(index, pathOne.Length - index);

            document.SaveToFile(pathOne.Replace("_enc.", "_dec."), FileFormat.Auto);
        }

        private void Encrypt(string inputFilePath, string outputFilePath)
        {
            using var encryptor = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(_dbQuery.GetUserKey((int)_userServiceAccessor.GetId), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            using var fsOutput = new FileStream(outputFilePath, FileMode.Create);
            using var cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write);
            using var fsInput = new FileStream(inputFilePath, FileMode.Open);
            int data;
            while ((data = fsInput.ReadByte()) != -1)
            {
                cs.WriteByte((byte)data);
            }
        }

        private void Decrypt(string inputFilePath, string outputFilePath)
        {
            using var encryptor = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(_dbQuery.GetUserKey((int)_userServiceAccessor.GetId), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            using var fsInput = new FileStream(inputFilePath, FileMode.Open);
            using var cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read);
            using var fsOutput = new FileStream(outputFilePath, FileMode.Create);
            int data;
            while ((data = cs.ReadByte()) != -1)
            {
                fsOutput.WriteByte((byte)data);
            }
        }
    }
}
