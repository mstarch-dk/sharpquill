using SharpQuill.Application.ContentPackage.DTOs;

namespace SharpQuill.Application.ContentPackage.Services;

public interface IContentPackageService
{
    Task<(Stream? Stream, string? Error)> ExportAsync(ExportRequestDto request);
    Task<(ImportValidationDto? Validation, string? Error)> ValidateAsync(Stream stream);
    Task<(ImportResultDto? Result, string? Error)> ImportAsync(Stream stream, int authorId);
}
