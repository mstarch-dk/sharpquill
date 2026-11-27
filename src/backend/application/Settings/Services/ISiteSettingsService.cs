using SharpQuill.Application.Settings.DTOs;

namespace SharpQuill.Application.Settings.Services;

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAllAsync();
    Task<SiteSettingsDto> UpdateAsync(SiteSettingsUpdateDto dto);
}
