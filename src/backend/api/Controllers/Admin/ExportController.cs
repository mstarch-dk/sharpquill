using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.ContentPackage.DTOs;
using SharpQuill.Application.ContentPackage.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/export")]
[Authorize(Policy = AuthPolicies.AdminRole)]
public class ExportController : ControllerBase
{
    private readonly IContentPackageService _packageService;

    public ExportController(IContentPackageService packageService)
    {
        _packageService = packageService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export([FromBody] ExportRequestDto? dto)
    {
        var request = dto ?? new ExportRequestDto(null, null);
        var (stream, error) = await _packageService.ExportAsync(request);

        if (stream == null)
        {
            return BadRequest(new ApiErrorResponse("EXPORT_FAILED", error ?? "Export failed"));
        }

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var filename = $"sharpquill-export-{timestamp}.zip";

        return File(stream, "application/zip", filename);
    }
}
