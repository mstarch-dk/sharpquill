using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Categories.DTOs;

namespace SharpQuill.Application.Articles.DTOs;

public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Content,
    RenderedLayoutDto? Layout,
    string? Excerpt,
    string? MetaTitle,
    string? MetaDescription,
    string? FeaturedImageUrl,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    AuthorDto? Author,
    CategoryDto? Category,
    IReadOnlyList<TagSummaryDto> Tags,
    ArticleSeriesInfoDto? Series,
    bool CommentsEnabled,
    bool RequiresSignIn,
    string? PageStyle = null,
    string? FeaturedImageMediumUrl = null,
    string? FeaturedImageDisplayUrl = null,
    string? FeaturedImageSize = null,
    string? AuthorName = null
);
