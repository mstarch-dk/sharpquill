namespace SharpQuill.Application.Auth.DTOs;

public record AuthorProfileDto(
    int Id,
    string Email,
    string Name,
    string Role,
    string? Bio,
    string? AvatarUrl
);
