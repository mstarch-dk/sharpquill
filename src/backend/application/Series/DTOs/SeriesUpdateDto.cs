namespace SharpQuill.Application.Series.DTOs;

public record SeriesUpdateDto(
    string Name,
    string Slug,
    string? Description,
    string? ThemeSlug
);
