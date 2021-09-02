﻿using System;
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
                .ForMember(c => c.UserDirectories, s => s.MapFrom(c => c.Directories.Select(d => d.DirectoryPath)))
                .ForMember(c => c.Files, s => s.MapFrom(c => c.Directories.Select(d => d.Files.Select(f => f.NameOfFile))));

            CreateMap<RegisterUserDto, User>()
                .ForMember(u => u.Email, c => c.MapFrom(ru => ru.Email))
                .ForMember(u => u.PasswordHash, s => s.MapFrom(ru => ru.Password));
        }
    }
}
