namespace SharpQuill.Application.Tags.DTOs;

public record TagDto(
    int Id,
    string Name,
    string Slug,
    int ArticleCount
);
