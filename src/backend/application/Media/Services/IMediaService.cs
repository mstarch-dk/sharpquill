using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.Media.DTOs;
using SharpQuill.Core.Common;

namespace SharpQuill.Application.Media.Services;

public interface IMediaService
{
    Task<MediaDto?> GetByIdAsync(int id);
    Task<PagedResultDto<MediaDto>> GetAllAsync(PaginationParams pagination);
    Task<(MediaUploadResultDto? Result, string? Error)> UploadAsync(Stream fileStream, string originalFilename, long size, int authorId);
    /// <summary>Null when the media does not exist.</summary>
    Task<IReadOnlyList<MediaUsageDto>?> GetUsagesAsync(int id);
    /// <summary>
    /// Deletes the record and its files (original, medium, thumbnail). Media that is still referenced is not deleted
    /// (status InUse) unless <paramref name="force"/> is set; a forced delete clears featured images (FK SET NULL) and
    /// leaves inline references broken.
    /// </summary>
    Task<MediaDeleteResult> DeleteAsync(int id, bool force = false);
    Task<int> RegenerateVariantsAsync();
}
