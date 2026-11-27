namespace SharpQuill.Application.Authors.DTOs;

public record AuthorAdminDto(
    int Id,
    string Email,
    string Name,
    string Role,
    string? Bio,
    string? AvatarUrl,
    string? CertificateThumbprint,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
