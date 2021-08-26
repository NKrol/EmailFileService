using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
