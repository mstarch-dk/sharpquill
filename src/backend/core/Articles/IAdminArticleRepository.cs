using SharpQuill.Core.Common;

namespace SharpQuill.Core.Articles;

public interface IAdminArticleRepository
{
    Task<Article?> GetByIdAsync(int id);
    Task<(IEnumerable<Article> Articles, int TotalCount)> GetAllAsync(PaginationParams pagination, ContentType? contentType = null, ArticleAdminFilter? filter = null);
    Task<Article> CreateAsync(Article article);
    Task<Article> UpdateAsync(Article article);
    Task<bool> DeleteAsync(int id);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    Task UpdateTagsAsync(int articleId, IEnumerable<int> tagIds);
    Task SetPublishedVersionAsync(int articleId, int? versionId);
}
