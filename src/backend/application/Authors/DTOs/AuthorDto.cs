namespace SharpQuill.Application.Authors.DTOs;

public record AuthorDto(
    int Id,
    string Name,
    string? AvatarUrl
);
