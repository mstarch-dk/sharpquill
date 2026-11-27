namespace SharpQuill.Application.Media.DTOs;

public record MediaUploadResultDto(
    int Id,
    string Filename,
    string OriginalFilename,
    string MimeType,
    long Size,
    string Url,
    string? ThumbnailUrl,
    string? MediumUrl
);
