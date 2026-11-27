namespace SharpQuill.Application.Series.DTOs;

public record SeriesDetailDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<SeriesArticleDto> Articles,
    string? ThemeSlug
);

public record SeriesArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    DateTime? PublishedAt,
    int Order
);
