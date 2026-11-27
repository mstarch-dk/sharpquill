using SharpQuill.Application.Comments.DTOs;
using FluentValidation;

namespace SharpQuill.Application.Comments.Validators;

public class CommentCreateValidator : AbstractValidator<CommentCreateDto>
{
    public CommentCreateValidator()
    {
        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.AuthorEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("A valid email address is required")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MaximumLength(5000).WithMessage("Comment must not exceed 5000 characters");

        RuleFor(x => x.Honeypot)
            .Empty().WithMessage("Invalid submission");
    }
}
