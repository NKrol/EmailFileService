using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmailFileService.Model
{
    public class MoveFileDto
    {
        public string Email { get; set; }
        public string ActualDirectory { get; set; }
        public string FileName { get; set; }
        public string DirectoryToMove { get; set; }
    }
}
