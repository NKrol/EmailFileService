using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Model
{
    public class Email
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Title { get; set; }

    }
}
