using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Entities;
using EmailFileService.Logic.FileManager;
using EmailFileService.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EmailFileService.Model.Validators
{
    public class ServiceFileOperationDtoValidator : AbstractValidator<ServiceFileOperationDto>
    {

        public ServiceFileOperationDtoValidator(EmailServiceDbContext dbContext, IUserServiceAccessor accessor)
        {
            RuleFor(d => d.OperationFile).Custom((file, context) =>
            {
                if (file == OperationFile.Move)
                {
                    var user = dbContext.Users.Include(f => f.Directories).ThenInclude(d => d.Files)
                        .FirstOrDefault(c => c.Id == accessor.GetId);
                    RuleFor(e => e.ActualFileDirectory).NotNull().MinimumLength(4).Custom(
                        (s, validationContext) =>
                        {
                            var any = user?.Directories.Any(f => f.DirectoryPath == s);
                            if (any == false) validationContext.AddFailure("You don't have this directory!");
                            RuleFor(e => e.FileName).NotEmpty().NotNull().Custom((s1, context1) =>
                            {
                                var haveThisFile = user.Directories.FirstOrDefault(f => f.DirectoryPath == s).Files
                                    .Any(e => e.NameOfFile == s1);
                                if (haveThisFile is false) context1.AddFailure("You don't have this file!");
                            });
                        }
                    );
                    RuleFor(e => e.NewFileDirectory).NotEmpty().NotNull();
                }
            });
        }

    }
}
