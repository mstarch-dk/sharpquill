namespace SharpQuill.Application.ContentPackage.DTOs;

public record ContentEntryDto(
    string Title,
    string Slug,
    string ContentType,
    string Status,
    string? Content,
    string? Layout,
    string? Excerpt,
    string? MetaTitle,
    string? MetaDescription,
    string? Category,
    IReadOnlyList<string>? Tags,
    SeriesEntryDto? Series,
    MediaEntryDto? FeaturedImage,
    DateTime? PublishedAt,
    DateTime CreatedAt
);
