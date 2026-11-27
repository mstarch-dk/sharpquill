namespace SharpQuill.Application.ContentPackage.DTOs;

public record ImportValidationDto(
    bool IsValid,
    ManifestDto? Manifest,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors
);
