using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Application.Comments.Services;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class CommentsController : ControllerBase
{
    private readonly IAdminCommentService _commentService;

    public CommentsController(IAdminCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CommentAdminDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CommentStatus? status = null)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _commentService.GetAllAsync(pagination, status);
        return Ok(new ApiResponse<IReadOnlyList<CommentAdminDto>>(result.Data, result.Meta));
    }

    [HttpGet("counts")]
    [ProducesResponseType(typeof(ApiResponse<CommentCountsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCounts()
    {
        var counts = await _commentService.GetCountsAsync();
        return Ok(new ApiResponse<CommentCountsDto>(counts));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CommentAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComment(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Comment not found"));
        }
        return Ok(new ApiResponse<CommentAdminDto>(comment));
    }

    [HttpPut("{id:int}/moderate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Moderate(int id, [FromBody] CommentModerationDto dto)
    {
        var (success, error) = await _commentService.ModerateAsync(id, dto.Status);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error!));
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _commentService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error!));
        }
        return NoContent();
    }
}
