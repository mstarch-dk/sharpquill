namespace SharpQuill.Application.Articles.DTOs;

public record ArticleVersionDto(
    int Id,
    int VersionNumber,
    DateTime CreatedAt
);

public record ArticleVersionDetailDto(
    int Id,
    int VersionNumber,
    string? Content,
    LayoutInputDto? Layout,
    string? Excerpt,
    string? MetaTitle,
    string? MetaDescription,
    DateTime CreatedAt,
    string? PageStyle = null,
    string? FeaturedImageSize = null
);
