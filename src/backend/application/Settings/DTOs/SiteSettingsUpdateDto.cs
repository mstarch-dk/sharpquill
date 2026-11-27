namespace SharpQuill.Application.Settings.DTOs;

public record SiteSettingsUpdateDto(
    string? SiteName,
    string? SiteDescription,
    string? SiteUrl,
    string? ContactEmail,
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
    // Contact details shown in the public site footer (contact_email stays the private contact form recipient)
    string? PublicEmail = null,
    string? PublicPhone = null,
    string? CompanyInfo = null
);
