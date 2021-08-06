using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmailFileService.Entities
{
    public class User : EntityBase
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public virtual IEnumerable<UserDirectory> Directories { get; set; }

    }
}
