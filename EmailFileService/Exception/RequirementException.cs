using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService.Exception
{
    public class RequirementException : System.Exception
    {
        public RequirementException(string message) : base(message)
        {
            
        }
    }
}
