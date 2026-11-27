using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.ContentPackage.DTOs;
using SharpQuill.Application.ContentPackage.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/import")]
[Authorize(Policy = AuthPolicies.AdminRole)]
[RequestSizeLimit(100 * 1024 * 1024)]
public class ImportController : ControllerBase
{
    private readonly IContentPackageService _packageService;

    public ImportController(IContentPackageService packageService)
    {
        _packageService = packageService;
    }

    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<ImportValidationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "No file provided"));
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var (validation, error) = await _packageService.ValidateAsync(stream);

        if (validation == null)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_FAILED", error ?? "Validation failed"));
        }

        return Ok(new ApiResponse<ImportValidationDto>(validation));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ImportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "No file provided"));
        }

        var authorId = User.GetAuthorId();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var (result, error) = await _packageService.ImportAsync(stream, authorId);

        if (result == null)
        {
            return BadRequest(new ApiErrorResponse("IMPORT_FAILED", error ?? "Import failed"));
        }

        return Ok(new ApiResponse<ImportResultDto>(result));
    }
}
