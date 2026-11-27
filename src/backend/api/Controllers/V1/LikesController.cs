using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Likes.DTOs;
using SharpQuill.Application.Likes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/articles/{articleId:int}/likes")]
public class LikesController : ControllerBase
{
    private readonly ILikeService _likeService;

    public LikesController(ILikeService likeService)
    {
        _likeService = likeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<LikeStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLikeStatus(int articleId, [FromQuery] string? visitorId)
    {
        var status = await _likeService.GetStatusAsync(articleId, visitorId);
        return Ok(new ApiResponse<LikeStatusDto>(status));
    }

    [HttpPost]
    [EnableRateLimiting("likes")]
    [ProducesResponseType(typeof(ApiResponse<LikeStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleLike(int articleId, [FromBody] LikeToggleDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var status = await _likeService.ToggleAsync(articleId, dto.VisitorId, ipAddress);
        if (status == null)
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Article not found"));

        return Ok(new ApiResponse<LikeStatusDto>(status));
    }
}
