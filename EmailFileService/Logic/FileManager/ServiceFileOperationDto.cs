using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Logic.FileManager
{
    public class ServiceFileOperationDto
    {
        public string ActualFileDirectory { get; set; }
        public string NewFileDirectory { get; set; }
        public string DirectoryName { get; set; }

        public string FileName { get; set; }
        public List<IFormFile> FormFiles { get; set; }
        public OperationFile OperationFile { get; set; }
    }
}
