using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.TweetPreviews.DTOs;
using SharpQuill.Application.TweetPreviews.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tweet-preview")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class TweetPreviewController : ControllerBase
{
    private readonly ITweetPreviewService _tweetPreviewService;

    public TweetPreviewController(ITweetPreviewService tweetPreviewService)
    {
        _tweetPreviewService = tweetPreviewService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TweetPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FetchPreview([FromBody] TweetPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "URL is required"));

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "Invalid URL. Must be an absolute HTTP or HTTPS URL."));
        }

        if (!request.Url.Contains("twitter.com") && !request.Url.Contains("x.com"))
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "URL must be a Twitter/X tweet URL."));
        }

        try
        {
            var preview = await _tweetPreviewService.FetchPreviewAsync(request.Url);
            return Ok(new ApiResponse<TweetPreviewDto>(preview));
        }
        catch (HttpRequestException)
        {
            return BadRequest(new ApiErrorResponse("FETCH_ERROR", "Could not fetch the tweet. The URL may be invalid or unreachable."));
        }
        catch (TaskCanceledException)
        {
            return BadRequest(new ApiErrorResponse("TIMEOUT_ERROR", "Request timed out while fetching the tweet."));
        }
    }
}

public class TweetPreviewRequest
{
    public string Url { get; set; } = "";
}
