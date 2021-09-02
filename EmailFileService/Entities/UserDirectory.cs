using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService.Entities
{
    public class UserDirectory : EntityBase
    {
        public string DirectoryPath { get; set; }
        public virtual IEnumerable<File> Files { get; set; }
        public bool IsMainDirectory { get; set; } = false;
    }
}
