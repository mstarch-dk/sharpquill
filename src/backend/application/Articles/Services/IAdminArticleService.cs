using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Services;

public interface IAdminArticleService
{
    Task<ArticleAdminDto?> GetByIdAsync(int id);
    Task<PagedResultDto<ArticleAdminDto>> GetAllAsync(PaginationParams pagination, ContentType? contentType = null, ArticleAdminFilter? filter = null);
    Task<(ArticleAdminDto? Article, string? Error)> CreateAsync(ArticleCreateDto dto, int? authorId);
    Task<(ArticleAdminDto? Article, string? Error)> UpdateAsync(int id, ArticleUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<IReadOnlyList<ArticleVersionDto>?> GetVersionsAsync(int articleId);
    Task<ArticleVersionDetailDto?> GetVersionAsync(int articleId, int versionId);
    Task<(ArticleAdminDto? Article, string? Error)> PublishVersionAsync(int articleId, int versionId);
    Task<ArticleDto?> GetPreviewAsync(int id);
}
