namespace SharpQuill.Application.Categories.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int ArticleCount
);
