using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.LinkPreviews.DTOs;
using SharpQuill.Application.LinkPreviews.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/link-preview")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class LinkPreviewController : ControllerBase
{
    private readonly ILinkPreviewService _linkPreviewService;

    public LinkPreviewController(ILinkPreviewService linkPreviewService)
    {
        _linkPreviewService = linkPreviewService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LinkPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FetchPreview([FromBody] LinkPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "URL is required"));

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "Invalid URL. Must be an absolute HTTP or HTTPS URL."));
        }

        try
        {
            var preview = await _linkPreviewService.FetchPreviewAsync(request.Url);
            return Ok(new ApiResponse<LinkPreviewDto>(preview));
        }
        catch (ArgumentException)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "Invalid URL. Must be an absolute HTTP or HTTPS URL."));
        }
        catch (HttpRequestException)
        {
            return BadRequest(new ApiErrorResponse("FETCH_ERROR", "Could not fetch the URL. The site may be unreachable."));
        }
        catch (TaskCanceledException)
        {
            return BadRequest(new ApiErrorResponse("TIMEOUT_ERROR", "Request timed out while fetching the URL."));
        }
    }
}

public class LinkPreviewRequest
{
    public string Url { get; set; } = "";
}
