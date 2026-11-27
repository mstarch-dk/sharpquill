namespace SharpQuill.Application.ContentPackage.DTOs;

public record MediaEntryDto(
    string Path,
    string OriginalFilename,
    string? AltText,
    string MimeType
);
