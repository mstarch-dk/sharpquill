using SharpQuill.Application.WebsiteUsers.DTOs;
using FluentValidation;

namespace SharpQuill.Application.WebsiteUsers.Validators;

public class WebsiteUserSignupValidator : AbstractValidator<WebsiteUserSignupDto>
{
    public WebsiteUserSignupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(320).WithMessage("Email must be at most 320 characters")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
