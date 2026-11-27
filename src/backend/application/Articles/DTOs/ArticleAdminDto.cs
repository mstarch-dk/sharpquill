using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.DTOs;

public record ArticleAdminDto(
    int Id,
    string Title,
    string Slug,
    string? Content,
    LayoutInputDto? Layout,
    string? Excerpt,
    string? MetaTitle,
    string? MetaDescription,
    int? FeaturedImageId,
    string? FeaturedImageUrl,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ArticleStatus Status,
    ContentType ContentType,
    AuthorDto? Author,
    CategoryDto? Category,
    IReadOnlyList<TagSummaryDto> Tags,
    ArticleSeriesSummaryDto? Series,
    int? SeriesOrder,
    int? VersionNumber,
    int TotalVersions,
    int? PublishedVersionId,
    bool CommentsEnabled,
    bool RequiresSignIn,
    string? PageStyle = null,
    string? FeaturedImageSize = null,
    string? FeaturedImageDisplayUrl = null,
    string? AuthorName = null
);
