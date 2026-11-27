namespace SharpQuill.Application.Authors.DTOs;

public record AuthorCreateDto(
    string Email,
    string Password,
    string Name,
    string Role,
    string? Bio,
    string? AvatarUrl,
    string? CertificateThumbprint
);
