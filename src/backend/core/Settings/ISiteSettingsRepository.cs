
namespace SharpQuill.Core.Settings;

public interface ISiteSettingsRepository
{
    Task<IEnumerable<SiteSettings>> GetAllAsync();
    Task<SiteSettings?> GetByKeyAsync(string key);
    Task<SiteSettings> UpsertAsync(string key, string? value);
    Task UpsertManyAsync(IDictionary<string, string?> settings);
}
