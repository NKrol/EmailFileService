using System.Collections.Generic;

namespace EmailFileService.Entities
{
    public class UserDirectory : EntityBase
    {
        public string DirectoryPath { get; set; }
        public virtual User User { get; set; }
        public int? ParentId { get; set; }
        public virtual UserDirectory Parent { get; set; }
        public virtual List<UserDirectory> Children { get; set; }
        public virtual IEnumerable<File> Files { get; set; }
        public bool IsMainDirectory { get; set; } = false;
    }
}
