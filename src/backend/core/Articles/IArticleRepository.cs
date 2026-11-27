using SharpQuill.Core.Common;

namespace SharpQuill.Core.Articles;

public interface IArticleRepository
{
    Task<Article?> GetBySlugAsync(string slug, ContentType contentType = ContentType.Article);
    Task<(IEnumerable<Article> Articles, int TotalCount)> GetPublishedAsync(PaginationParams pagination);
    Task<(IEnumerable<Article> Articles, int TotalCount)> GetByCategorySlugAsync(string categorySlug, PaginationParams pagination);
    Task<(IEnumerable<Article> Articles, int TotalCount)> GetByTagSlugAsync(string tagSlug, PaginationParams pagination);
    Task<IEnumerable<Article>> GetFeaturedAsync(int count = 5);

    /// <summary>
    /// Full-text search over published articles (not pages), best match first. <paramref name="query"/> is parsed
    /// with websearch_to_tsquery, so any input is valid. Members-only articles are included only when
    /// <paramref name="includeMembersOnly"/> is set.
    /// </summary>
    Task<(IReadOnlyList<ArticleSearchHit> Hits, int TotalCount)> SearchPublishedAsync(
        string query, bool includeMembersOnly, PaginationParams pagination);
}
