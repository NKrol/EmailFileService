using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService
{
    public class Authentication
    {
        public string JwtIssuer { get; set; }
        public string JwtKey { get; set; }
        public int JwtExpire { get; set; }
    }
}
