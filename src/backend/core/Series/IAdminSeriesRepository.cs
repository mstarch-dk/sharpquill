using SharpQuill.Core.Articles;

namespace SharpQuill.Core.Series;

public interface IAdminSeriesRepository
{
    Task<ArticleSeries?> GetByIdAsync(int id);
    Task<IEnumerable<ArticleSeries>> GetAllAsync();
    Task<ArticleSeries> CreateAsync(ArticleSeries series);
    Task<ArticleSeries> UpdateAsync(ArticleSeries series);
    Task<bool> DeleteAsync(int id);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    Task<int> GetArticleCountAsync(int seriesId);
    Task<IEnumerable<Article>> GetArticlesInSeriesAsync(int seriesId);
    Task SetArticleSeriesAsync(int articleId, int seriesId, int order);
    Task RemoveArticleFromSeriesAsync(int articleId);
    Task ReorderArticlesAsync(int seriesId, IReadOnlyList<int> articleIds);
}
