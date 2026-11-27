using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Media.DTOs;
using SharpQuill.Application.Media.Services;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/media")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MediaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _mediaService.GetAllAsync(pagination);
        return Ok(new ApiResponse<IReadOnlyList<MediaDto>>(result.Data, result.Meta));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaById(int id)
    {
        var media = await _mediaService.GetByIdAsync(id);
        if (media == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Media with id '{id}' not found"));
        }
        return Ok(new ApiResponse<MediaDto>(media));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MediaUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "No file provided"));
        }

        var authorId = User.GetAuthorId();

        await using var stream = file.OpenReadStream();
        var (result, error) = await _mediaService.UploadAsync(
            stream,
            file.FileName,
            file.Length,
            authorId
        );

        if (result == null)
        {
            return BadRequest(new ApiErrorResponse("UPLOAD_FAILED", error ?? "Failed to upload file"));
        }

        return CreatedAtAction(nameof(GetMediaById), new { id = result.Id }, new ApiResponse<MediaUploadResultDto>(result));
    }

    [HttpPost("regenerate-variants")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateVariants()
    {
        var count = await _mediaService.RegenerateVariantsAsync();
        return Ok(new ApiResponse<object>(new { processed = count }));
    }

    /// <summary>Where the media is still referenced (featured images, content, layouts, settings, avatars).</summary>
    [HttpGet("{id:int}/usages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MediaUsageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaUsages(int id)
    {
        var usages = await _mediaService.GetUsagesAsync(id);
        if (usages == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Media with id '{id}' not found"));
        }
        return Ok(new ApiResponse<IReadOnlyList<MediaUsageDto>>(usages));
    }

    /// <summary>
    /// Deletes the media and its files. Answers 409 MEDIA_IN_USE (usages in the error details) while it is still
    /// referenced, unless force=true.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMedia(int id, [FromQuery] bool force = false)
    {
        var result = await _mediaService.DeleteAsync(id, force);

        return result.Status switch
        {
            MediaDeleteStatus.NotFound => NotFound(new ApiErrorResponse("NOT_FOUND", "Media not found")),
            MediaDeleteStatus.InUse => Conflict(new ApiErrorResponse(
                "MEDIA_IN_USE",
                "The media is still in use. Remove it from these places first, or delete with force=true.",
                result.Usages.Select(u => new ApiErrorDetail(u.Kind, u.Title)))),
            _ => NoContent()
        };
    }
}
