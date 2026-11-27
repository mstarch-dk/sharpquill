namespace SharpQuill.Application.Categories.DTOs;

public record CategoryUpdateDto(
    string Name,
    string Slug,
    string? Description
);
