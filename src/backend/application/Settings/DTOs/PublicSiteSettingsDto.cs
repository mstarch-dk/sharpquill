namespace SharpQuill.Application.Settings.DTOs;

// What the unauthenticated GET /api/v1/settings returns: an allow-list of settings meant to be public.
// Private settings (contact_email, the contact form recipient) stay admin-only; add new fields here only when the
// public website or the admin login page needs them before sign-in.
public record PublicSiteSettingsDto(
    string? SiteName,
    string? SiteDescription,
    string? SiteUrl,
    string? SocialTwitter,
    string? SocialFacebook,
    string? SocialInstagram,
    string? SocialLinkedIn,
    string? MetaKeywords,
    string? Theme,
    string? GlobalFont,
    string? Language,
    string? FooterText,
    bool DarkModeAdmin,
    bool DarkModeWeb,
    bool LoginEnabled,
    string? PublicEmail,
    string? PublicPhone,
    string? CompanyInfo
)
{
    public static PublicSiteSettingsDto From(SiteSettingsDto s) => new(
        s.SiteName,
        s.SiteDescription,
        s.SiteUrl,
        s.SocialTwitter,
        s.SocialFacebook,
        s.SocialInstagram,
        s.SocialLinkedIn,
        s.MetaKeywords,
        s.Theme,
        s.GlobalFont,
        s.Language,
        s.FooterText,
        s.DarkModeAdmin,
        s.DarkModeWeb,
        s.LoginEnabled,
        s.PublicEmail,
        s.PublicPhone,
        s.CompanyInfo
    );
}
