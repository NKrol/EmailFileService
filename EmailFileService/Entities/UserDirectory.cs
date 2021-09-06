using System.Collections.Generic;

namespace EmailFileService.Entities
{
    public class UserDirectory : EntityBase
    {
        public string DirectoryPath { get; set; }
        public virtual IEnumerable<File> Files { get; set; }
        public bool IsMainDirectory { get; set; } = false;
    }
}
