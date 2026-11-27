using SharpQuill.Application.Common;
using SharpQuill.Application.Settings.DTOs;
using FluentValidation;

namespace SharpQuill.Application.Settings.Validators;

public class SiteSettingsUpdateValidator : AbstractValidator<SiteSettingsUpdateDto>
{
    private static readonly HashSet<string> AllowedFontIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "inter",
        "manrope",
        "source-serif-4",
        "atkinson-hyperlegible"
    };

    private static readonly System.Text.RegularExpressions.Regex LanguageTag =
        new(@"^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public SiteSettingsUpdateValidator()
    {
        // These values end up as links on the public site
        foreach (var (name, selector) in new (string, System.Linq.Expressions.Expression<Func<SiteSettingsUpdateDto, string?>>)[]
        {
            ("Site URL", x => x.SiteUrl),
            ("Twitter", x => x.SocialTwitter),
            ("Facebook", x => x.SocialFacebook),
            ("Instagram", x => x.SocialInstagram),
            ("LinkedIn", x => x.SocialLinkedIn)
        })
        {
            RuleFor(selector)
                .Must(UrlSafety.IsSafe)
                .WithMessage($"{name} must be an http(s) URL or a relative path");
        }

        // Used as the <html lang> attribute: a BCP 47 style tag such as "da", "en" or "en-GB"
        RuleFor(x => x.Language)
            .Must(language => string.IsNullOrWhiteSpace(language) || LanguageTag.IsMatch(language))
            .WithMessage("Language must be a language tag such as 'da', 'en' or 'en-GB'");

        RuleFor(x => x.SiteName).MaximumLength(100);
        RuleFor(x => x.SiteDescription).MaximumLength(500);
        RuleFor(x => x.FooterText).MaximumLength(500);

        // Footer contact details: rendered as mailto:/tel: links, so keep them to plain address/number characters
        RuleFor(x => x.PublicEmail)
            .MaximumLength(200)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PublicEmail))
            .WithMessage("Public email must be an email address");
        RuleFor(x => x.PublicPhone)
            .MaximumLength(40)
            .Matches(@"^[0-9+()\s.-]*$")
            .WithMessage("Public phone may only contain digits, spaces and + ( ) . -");
        RuleFor(x => x.CompanyInfo).MaximumLength(200);

        RuleFor(x => x.GlobalFont)
            .Must(fontId => string.IsNullOrWhiteSpace(fontId) || AllowedFontIds.Contains(fontId))
            .WithMessage("Global font must be one of: inter, manrope, source-serif-4, atkinson-hyperlegible");
    }
}
