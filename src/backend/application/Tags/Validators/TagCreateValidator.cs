using SharpQuill.Application.Tags.DTOs;
using FluentValidation;

namespace SharpQuill.Application.Tags.Validators;

public class TagCreateValidator : AbstractValidator<TagCreateDto>
{
    public TagCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(50).WithMessage("Slug must not exceed 50 characters")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens");
    }
}
