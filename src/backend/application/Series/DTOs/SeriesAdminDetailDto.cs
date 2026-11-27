using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Series.DTOs;

public record SeriesAdminDetailDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int ArticleCount,
    IReadOnlyList<SeriesArticleAdminDto> Articles,
    string? ThemeSlug
);

public record SeriesArticleAdminDto(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    DateTime? PublishedAt,
    ArticleStatus Status,
    string AuthorName,
    int Order
);
