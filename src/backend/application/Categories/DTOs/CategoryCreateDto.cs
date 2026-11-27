namespace SharpQuill.Application.Categories.DTOs;

public record CategoryCreateDto(
    string Name,
    string Slug,
    string? Description
);
