using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Series.DTOs;

namespace SharpQuill.Application.Series.Services;

public interface ISeriesService
{
    Task<IReadOnlyList<SeriesDto>> GetAllAsync();
    Task<SeriesDetailDto?> GetBySlugAsync(string slug);
    Task<ArticleSeriesInfoDto?> GetSeriesInfoForArticleAsync(int articleId, int seriesId);
}
