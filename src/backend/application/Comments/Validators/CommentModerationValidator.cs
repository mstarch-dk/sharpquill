using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Core.Comments;
using FluentValidation;

namespace SharpQuill.Application.Comments.Validators;

public class CommentModerationValidator : AbstractValidator<CommentModerationDto>
{
    public CommentModerationValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s == CommentStatus.Approved || s == CommentStatus.Rejected)
            .WithMessage("Status must be Approved or Rejected");
    }
}
