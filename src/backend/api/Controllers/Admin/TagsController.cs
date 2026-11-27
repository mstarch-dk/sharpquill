using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Tags.DTOs;
using SharpQuill.Application.Tags.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tags")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class TagsController : ControllerBase
{
    private readonly IAdminTagService _tagService;

    public TagsController(IAdminTagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _tagService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<TagDto>>(tags));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag(int id)
    {
        var tag = await _tagService.GetByIdAsync(id);
        if (tag == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Tag with id '{id}' not found"));
        }
        return Ok(new ApiResponse<TagDto>(tag));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateDto dto)
    {
        var (tag, error) = await _tagService.CreateAsync(dto);

        if (tag == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create tag"));
        }

        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, new ApiResponse<TagDto>(tag));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] TagUpdateDto dto)
    {
        var (tag, error) = await _tagService.UpdateAsync(id, dto);

        if (tag == null)
        {
            if (error == "Tag not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update tag"));
        }

        return Ok(new ApiResponse<TagDto>(tag));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var (success, error) = await _tagService.DeleteAsync(id);

        if (!success)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error ?? $"Tag with id '{id}' not found"));
        }

        return NoContent();
    }
}
