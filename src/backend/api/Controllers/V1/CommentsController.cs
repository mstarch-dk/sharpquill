using SharpQuill.Api.Infrastructure.Auth;
using System.Threading.RateLimiting;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Application.Comments.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/articles/{articleId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetComments(int articleId)
    {
        if (await IsGatedForCallerAsync(articleId))
        {
            return SignInRequired();
        }

        var comments = await _commentService.GetApprovedByArticleIdAsync(articleId);
        return Ok(new ApiResponse<IReadOnlyList<CommentDto>>(comments));
    }

    [HttpPost]
    [EnableRateLimiting("comments")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateComment(int articleId, [FromBody] CommentCreateDto dto)
    {
        if (await IsGatedForCallerAsync(articleId))
        {
            return SignInRequired();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (comment, error) = await _commentService.CreateAsync(articleId, dto, ipAddress);

        if (error != null)
        {
            if (error.Contains("not found"))
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            return BadRequest(new ApiErrorResponse("BAD_REQUEST", error));
        }

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<CommentDto>(comment!));
    }

    // Comments on a members-only article are members-only too
    private async Task<bool> IsGatedForCallerAsync(int articleId) =>
        !User.IsWebsiteUser() && await _commentService.RequiresSignInAsync(articleId);

    private IActionResult SignInRequired() =>
        Unauthorized(new ApiErrorResponse("SIGN_IN_REQUIRED", "This article requires sign-in to view"));
}
