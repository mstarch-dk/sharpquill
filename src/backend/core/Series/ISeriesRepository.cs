using SharpQuill.Core.Articles;

namespace SharpQuill.Core.Series;

public interface ISeriesRepository
{
    Task<IEnumerable<ArticleSeries>> GetAllWithArticleCountAsync();
    Task<ArticleSeries?> GetBySlugAsync(string slug);
    Task<IEnumerable<Article>> GetArticlesBySeriesIdAsync(int seriesId);
}
