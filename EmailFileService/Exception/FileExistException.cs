using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EmailFileService.Exception
{
    public class FileExistException : System.Exception
    {
        public FileExistException(string message) : base(message)
        {

            
        }
    }
}
