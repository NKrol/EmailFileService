﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmailFileService.Exception;
using Microsoft.EntityFrameworkCore;

namespace EmailFileService.Entities
{
    public class EmailServiceDbContext : DbContext
    {

        public EmailServiceDbContext(DbContextOptions<EmailServiceDbContext> options) : base(options)
        {
            
        }

        public DbSet<File> Files { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserDirectory> UserDirectories { get; set; }
        public DbSet<Keys> Keys { get; set; }

        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            modelBuilder.Entity<UserDirectory>()
                .Property(ud => ud.DirectoryPath)
                .IsRequired();

        }

        public User FindUser(int? userId, string directory, string fileName)
        {
            var query = Users.Include(u => u.Directories.Where(x => x.DirectoryPath == directory))
                .ThenInclude(d => d.Files.Where(x =>
                    x.NameOfFile == fileName & x.IsActive == true & x.OperationType == OperationType.Create |
                    x.OperationType == OperationType.Modify))
                .AsSingleQuery()
                .Single(u => u.Id == userId);

            var fileExist = query.Directories.FirstOrDefault(d => d.DirectoryPath == directory).Files
                .Any(x => x.NameOfFile == fileName & x.IsActive == true & x.OperationType == OperationType.Create |
                          x.OperationType == OperationType.Modify);

            if (!fileExist) throw new NotFoundException("This file not exist!");
            
            return query;
        }
    }
}
