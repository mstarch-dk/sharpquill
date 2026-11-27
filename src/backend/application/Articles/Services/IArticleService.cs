using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Services;

public interface IArticleService
{
    Task<ArticleDto?> GetBySlugAsync(string slug, ContentType contentType = ContentType.Article);
    Task<PagedResultDto<ArticleListDto>> GetPublishedAsync(PaginationParams pagination);
    Task<PagedResultDto<ArticleListDto>> GetByCategorySlugAsync(string categorySlug, PaginationParams pagination);
    Task<PagedResultDto<ArticleListDto>> GetByTagSlugAsync(string tagSlug, PaginationParams pagination);
    Task<IReadOnlyList<ArticleListDto>> GetFeaturedAsync(int count = 5);

    /// <summary>
    /// Full-text search over published articles, best match first. A blank query returns an empty page; members-only
    /// articles are included only when <paramref name="includeMembersOnly"/> is set (a signed-in reader).
    /// </summary>
    Task<PagedResultDto<SearchResultDto>> SearchAsync(string? query, bool includeMembersOnly, PaginationParams pagination);
}
