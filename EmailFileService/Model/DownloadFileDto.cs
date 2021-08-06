using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailFileService.Model
{
    public class DownloadFileDto
    {
        public string PathToFile { get; set; }
        public string ExtensionFile { get; set; }
    }
}
