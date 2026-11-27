namespace SharpQuill.Application.Series.DTOs;

public record SeriesDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int ArticleCount,
    string? ThemeSlug
);
