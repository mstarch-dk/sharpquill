using SharpQuill.Application.Settings.DTOs;
using SharpQuill.Core.Settings;

namespace SharpQuill.Application.Settings.Services;

public class SiteSettingsService : ISiteSettingsService
{
    private readonly ISiteSettingsRepository _settingsRepository;

    // Keys for site settings
    private const string KeySiteName = "site_name";
    private const string KeySiteDescription = "site_description";
    private const string KeySiteUrl = "site_url";
    private const string KeyContactEmail = "contact_email";
    private const string KeySocialTwitter = "social_twitter";
    private const string KeySocialFacebook = "social_facebook";
    private const string KeySocialInstagram = "social_instagram";
    private const string KeySocialLinkedIn = "social_linkedin";
    private const string KeyMetaKeywords = "meta_keywords";
    private const string KeyTheme = "theme";
    private const string KeyGlobalFont = "global_font";
    private const string KeyDarkModeAdmin = "dark_mode_admin";
    private const string KeyDarkModeWeb = "dark_mode_web";
    private const string KeyLoginEnabled = "login_enabled";
    private const string KeyLanguage = "language";
    private const string KeyFooterText = "footer_text";
    private const string KeyPublicEmail = "public_email";
    private const string KeyPublicPhone = "public_phone";
    private const string KeyCompanyInfo = "company_info";

    public SiteSettingsService(ISiteSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<SiteSettingsDto> GetAllAsync()
    {
        var settings = (await _settingsRepository.GetAllAsync()).ToDictionary(s => s.Key, s => s.Value);

        return new SiteSettingsDto(
            GetValue(settings, KeySiteName),
            GetValue(settings, KeySiteDescription),
            GetValue(settings, KeySiteUrl),
            GetValue(settings, KeyContactEmail),
            GetValue(settings, KeySocialTwitter),
            GetValue(settings, KeySocialFacebook),
            GetValue(settings, KeySocialInstagram),
            GetValue(settings, KeySocialLinkedIn),
            GetValue(settings, KeyMetaKeywords),
            GetValue(settings, KeyTheme),
            GetValue(settings, KeyGlobalFont),
            GetValue(settings, KeyLanguage),
            GetValue(settings, KeyFooterText),
            GetBoolValue(settings, KeyDarkModeAdmin),
            GetBoolValue(settings, KeyDarkModeWeb),
            GetBoolValue(settings, KeyLoginEnabled, defaultValue: true),
            GetValue(settings, KeyPublicEmail),
            GetValue(settings, KeyPublicPhone),
            GetValue(settings, KeyCompanyInfo)
        );
    }

    public async Task<SiteSettingsDto> UpdateAsync(SiteSettingsUpdateDto dto)
    {
        var settings = new Dictionary<string, string?>
        {
            [KeySiteName] = dto.SiteName,
            [KeySiteDescription] = dto.SiteDescription,
            [KeySiteUrl] = dto.SiteUrl,
            [KeyContactEmail] = dto.ContactEmail,
            [KeySocialTwitter] = dto.SocialTwitter,
            [KeySocialFacebook] = dto.SocialFacebook,
            [KeySocialInstagram] = dto.SocialInstagram,
            [KeySocialLinkedIn] = dto.SocialLinkedIn,
            [KeyMetaKeywords] = dto.MetaKeywords,
            [KeyTheme] = dto.Theme,
            [KeyGlobalFont] = dto.GlobalFont,
            [KeyLanguage] = dto.Language,
            [KeyFooterText] = dto.FooterText,
            [KeyDarkModeAdmin] = dto.DarkModeAdmin.ToString().ToLowerInvariant(),
            [KeyDarkModeWeb] = dto.DarkModeWeb.ToString().ToLowerInvariant(),
            [KeyLoginEnabled] = dto.LoginEnabled.ToString().ToLowerInvariant(),
            [KeyPublicEmail] = dto.PublicEmail,
            [KeyPublicPhone] = dto.PublicPhone,
            [KeyCompanyInfo] = dto.CompanyInfo
        };

        await _settingsRepository.UpsertManyAsync(settings);

        return await GetAllAsync();
    }

    private static string? GetValue(Dictionary<string, string?> settings, string key)
    {
        return settings.TryGetValue(key, out var value) ? value : null;
    }

    private static bool GetBoolValue(Dictionary<string, string?> settings, string key, bool defaultValue = false)
    {
        if (!settings.TryGetValue(key, out var value))
            return defaultValue;
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
