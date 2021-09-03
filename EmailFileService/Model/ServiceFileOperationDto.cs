using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using EmailFileService.Model.Logic;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Model
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
