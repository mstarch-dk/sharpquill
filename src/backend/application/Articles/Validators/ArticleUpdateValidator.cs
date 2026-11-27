using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using FluentValidation;

namespace SharpQuill.Application.Articles.Validators;

public class ArticleUpdateValidator : AbstractValidator<ArticleUpdateDto>
{
    public ArticleUpdateValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens");

        RuleFor(x => x.Excerpt)
            .MaximumLength(500).WithMessage("Excerpt must not exceed 500 characters")
            .When(x => x.Excerpt != null);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(70).WithMessage("Meta title must not exceed 70 characters")
            .When(x => x.MetaTitle != null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(160).WithMessage("Meta description must not exceed 160 characters")
            .When(x => x.MetaDescription != null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid article status");

        RuleFor(x => x.Layout)
            .Must(layout => LayoutComposer.IsValidLayout(layout))
            .WithMessage("Invalid layout structure")
            .When(x => x.Layout != null);

        RuleFor(x => x.FeaturedImageSize)
            .Must(size => size is null || size is "extra-small" or "small" or "medium" or "original")
            .WithMessage("Featured image size must be one of: small, medium, original");

        RuleFor(x => x.AuthorName)
            .MaximumLength(200).WithMessage("Author name must not exceed 200 characters")
            .When(x => x.AuthorName != null);
    }
}
