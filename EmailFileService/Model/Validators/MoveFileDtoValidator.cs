using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Entities;
using EmailFileService.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EmailFileService.Model.Validators
{
    public class MoveFileDtoValidator : AbstractValidator<MoveFileDto>
    {
        public MoveFileDtoValidator(EmailServiceDbContext dbContext, IUserServiceAccessor serviceAccessor)
        {
            var user = dbContext.Users.Include(f => f.Directories).ThenInclude(d => d.Files)
                .FirstOrDefault(c => c.Id == serviceAccessor.GetId);
            RuleFor(e => e.ActualDirectory).NotNull().MinimumLength(4).Custom(
                (s, validationContext) =>
                {
                    var any = user?.Directories.FirstOrDefault(f => f.DirectoryPath == s);
                    if (any is null)
                    {
                        validationContext.AddFailure("You don't have this directory!");
                    }
                    // else
                    // {
                    //     RuleFor(e => e.FileName).NotEmpty().NotNull().Custom((s1, context1) =>
                    //         {
                    //             var haveThisFile = user?.Directories.FirstOrDefault(f => f.DirectoryPath == s)?.Files
                    //                                     .FirstOrDefault(e => e.NameOfFile == s1);
                    //             if (haveThisFile is null) context1.AddFailure("You don't have this file!");
                    //         });
                    // }
                }
            );
            RuleFor(e => e.DirectoryToMove).NotEmpty().NotNull();
        }
    }
}
