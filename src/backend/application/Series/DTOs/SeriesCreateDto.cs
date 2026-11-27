namespace SharpQuill.Application.Series.DTOs;

public record SeriesCreateDto(
    string Name,
    string Slug,
    string? Description,
    string? ThemeSlug
);
