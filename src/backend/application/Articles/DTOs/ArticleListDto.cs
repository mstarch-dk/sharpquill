using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Categories.DTOs;

namespace SharpQuill.Application.Articles.DTOs;

public record ArticleListDto(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? FeaturedImageUrl,
    DateTime? PublishedAt,
    AuthorDto? Author,
    CategoryDto? Category,
    IReadOnlyList<TagSummaryDto> Tags,
    ArticleSeriesSummaryDto? Series,
    bool RequiresSignIn,
    string? FeaturedImageThumbnailUrl = null,
    string? AuthorName = null
);

public record TagSummaryDto(
    int Id,
    string Name,
    string Slug
);
