using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EmailFileService.Exception
{
    public class CreatedAccountException : System.Exception
    {

        public CreatedAccountException(string message) : base(message)
        {
            
        }

    }
}
