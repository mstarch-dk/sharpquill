namespace SharpQuill.Application.Authors.DTOs;

public record AuthorUpdateDto(
    string Email,
    string Name,
    string Role,
    string? Password,
    string? Bio,
    string? AvatarUrl,
    string? CertificateThumbprint
);
