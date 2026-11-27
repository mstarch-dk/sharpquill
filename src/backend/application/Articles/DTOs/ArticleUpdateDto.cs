using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.DTOs;

public record ArticleUpdateDto(
    string Title,
    string Slug,
    string? Content,
    LayoutInputDto? Layout,
    string? Excerpt,
    string? MetaTitle,
    string? MetaDescription,
    int? CategoryId,
    int? FeaturedImageId,
    IReadOnlyList<int>? TagIds,
    ArticleStatus Status,
    ContentType ContentType = ContentType.Article,
    bool CommentsEnabled = true,
    bool RequiresSignIn = false,
    string? PageStyle = null,
    string? FeaturedImageSize = null,
    string? AuthorName = null,
    DateTime? PublishedAt = null
);
