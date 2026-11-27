using SharpQuill.Core.Common;

namespace SharpQuill.Core.Media;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(int id);
    Task<IEnumerable<MediaItem>> GetByIdsAsync(IEnumerable<int> ids);
    Task<(IEnumerable<MediaItem> Items, int TotalCount)> GetAllAsync(PaginationParams pagination);
    Task<MediaItem> CreateAsync(MediaItem media);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<MediaUsage>> GetUsagesAsync(MediaItem media);
    Task<IEnumerable<MediaItem>> GetRasterMediaWithoutVariantsAsync();
    Task UpdateVariantPathsAsync(int id, string? thumbnailPath, string? mediumPath, int? width, int? height);
}
