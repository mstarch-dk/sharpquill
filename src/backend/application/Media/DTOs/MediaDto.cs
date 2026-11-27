namespace SharpQuill.Application.Media.DTOs;

public record MediaDto(
    int Id,
    string Filename,
    string OriginalFilename,
    string MimeType,
    long Size,
    string Url,
    string? ThumbnailUrl,
    string? MediumUrl,
    int? Width,
    int? Height,
    string? AltText,
    DateTime CreatedAt,
    MediaAuthorDto Author
);

public record MediaAuthorDto(
    int Id,
    string Name
);
