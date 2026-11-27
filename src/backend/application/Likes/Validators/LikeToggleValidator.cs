using SharpQuill.Application.Likes.DTOs;
using FluentValidation;

namespace SharpQuill.Application.Likes.Validators;

public class LikeToggleValidator : AbstractValidator<LikeToggleDto>
{
    public LikeToggleValidator()
    {
        RuleFor(x => x.VisitorId)
            .NotEmpty().WithMessage("Visitor ID is required")
            .Matches(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
            .WithMessage("Visitor ID must be a valid UUID");
    }
}
