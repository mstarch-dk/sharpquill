using SharpQuill.Application.Series.DTOs;

namespace SharpQuill.Application.Series.Services;

public interface IAdminSeriesService
{
    Task<SeriesAdminDetailDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<SeriesDto>> GetAllAsync();
    Task<(SeriesDto? Series, string? Error)> CreateAsync(SeriesCreateDto dto);
    Task<(SeriesDto? Series, string? Error)> UpdateAsync(int id, SeriesUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? Error)> AddArticleAsync(int seriesId, SeriesAddArticleDto dto);
    Task<(bool Success, string? Error)> RemoveArticleAsync(int seriesId, int articleId);
    Task<(bool Success, string? Error)> ReorderArticlesAsync(int seriesId, SeriesReorderDto dto);
}
