using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService.Exception
{
    public class ForbidException : System.Exception
    {
        public ForbidException(string message) : base(message)
        {

        }
    }
}
