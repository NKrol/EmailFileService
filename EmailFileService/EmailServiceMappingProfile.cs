using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DocumentFormat.OpenXml.Drawing.ChartDrawing;
using EmailFileService.Entities;
using EmailFileService.Model;

namespace EmailFileService
{
    public class EmailServiceMappingProfile : Profile
    {
        public EmailServiceMappingProfile()
        {
            CreateMap<User, ShowMyFilesDto>()
                .ForMember(c => c.UserDirectories, s => s.MapFrom(c => c.Directories.SelectMany(d => d.DirectoryPath)))
                .ForMember(c => c.Files, s => s.MapFrom(c => c.Directories.SelectMany(d => d.Files.Select(f => f.NameOfFile))));

            //CreateMap<RegisterUserDto, User>()
            //    .ForMember(u => u.Email, c => c.MapFrom(ru => ru.Email))
            //    .ForMember(u => u.PasswordHash, s => s.MapFrom(ru => ru.Password));


            CreateMap<RegisterUserDto, User>()
                .ForMember(d => d.Email, c => c.MapFrom(d => d.Email))
                .ForMember(d => d.Directories, c => c.MapFrom(x => new List<UserDirectory>()
                {
                    new UserDirectory()
                    {
                        DirectoryPath = GeneratePath(x.Email),
                        IsMainDirectory = true,
                        Files = new List<File>()
                    }
                }))
                .ForMember(d => d.Email, c => c.MapFrom(d => d.Email))
                .ForMember(d => d.Keys, c => c.MapFrom(d => new Keys()
                {
                    Key = GenerateKey()
                }));



        }

        private string GeneratePath(string email)
        {
            var path = email.Replace('@', '_').Replace('@', '_');

            return path;
        }
        private static string GenerateKey()
        {
            Random random = new();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
