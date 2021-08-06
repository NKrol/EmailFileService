using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmailFileService.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace EmailFileService.Model.Validators
{
    public class RegisterValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterValidator(EmailServiceDbContext dbContext)
        {
            RuleFor(u => u.Email).EmailAddress().NotEmpty();

            RuleFor(u => u.Email).Custom((value, context) =>
            {
                var inUse = dbContext.Users.Any(u => u.Email == value);
                if (inUse)
                {
                    context.AddFailure("Email in use");
                }
            });

            RuleFor(u => u.Password).Equal(u => u.ConfirmedPassword).MinimumLength(8);
        }
    }
}
