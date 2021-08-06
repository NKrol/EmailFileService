using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Entities;

namespace EmailFileService.Model
{
    public class ShowMyFilesDto
    {
        public string UserDirectories { get; set; }
        public IEnumerable<string> Files { get; set; }
    }
}
