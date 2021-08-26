using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace EmailFileService.Model.Validators
{
    public class EmailSendValidator : AbstractValidator<Email>
    {

        public EmailSendValidator(EmailServiceDbContext dbContext)
        {
            RuleFor(u => u.Receiver).Equal("filesservice@api.com").EmailAddress();

            RuleFor(e => e.Sender).EmailAddress();

            RuleFor(e => e.Sender).Custom(((value, context) =>
            {
                var emailExist = dbContext.Users.First(c => c.Email == value);
                if (emailExist is null)
                {
                    context.AddFailure("Email isn't exist!");
                }
            }));
        }
    }
}
