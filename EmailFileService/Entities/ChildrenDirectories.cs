using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService.Entities
{
    public class ChildrenDirectories : EntityBase
    {
        public string DirectoryPath { get; set; }
        public int? ParentId { get; set; }
        public virtual UserDirectory Parent { get; set; }
        public virtual IEnumerable<File> Files { get; set; }
    }
}
