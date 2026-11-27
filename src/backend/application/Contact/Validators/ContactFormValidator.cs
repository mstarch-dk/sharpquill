using SharpQuill.Application.Contact.DTOs;
using FluentValidation;

namespace SharpQuill.Application.Contact.Validators;

public class ContactFormValidator : AbstractValidator<ContactFormDto>
{
    public ContactFormValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}
