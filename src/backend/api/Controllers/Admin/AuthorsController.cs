using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Authors.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/authors")]
[Authorize(Policy = AuthPolicies.AdminRole)]
public class AuthorsController : ControllerBase
{
    private readonly IAdminAuthorService _authorService;

    public AuthorsController(IAdminAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuthorAdminDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthors()
    {
        var authors = await _authorService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<AuthorAdminDto>>(authors));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AuthorAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuthor(int id)
    {
        var author = await _authorService.GetByIdAsync(id);
        if (author == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Author with id '{id}' not found"));
        }
        return Ok(new ApiResponse<AuthorAdminDto>(author));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AuthorAdminDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAuthor([FromBody] AuthorCreateDto dto)
    {
        var (author, error) = await _authorService.CreateAsync(dto);

        if (author == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create author"));
        }

        return CreatedAtAction(nameof(GetAuthor), new { id = author.Id }, new ApiResponse<AuthorAdminDto>(author));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AuthorAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAuthor(int id, [FromBody] AuthorUpdateDto dto)
    {
        var requestingAuthorId = User.GetAuthorId();
        var (author, error) = await _authorService.UpdateAsync(id, dto, requestingAuthorId);

        if (author == null)
        {
            if (error == "Author not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update author"));
        }

        return Ok(new ApiResponse<AuthorAdminDto>(author));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAuthor(int id)
    {
        var requestingAuthorId = User.GetAuthorId();
        var (success, error) = await _authorService.DeleteAsync(id, requestingAuthorId);

        if (!success)
        {
            if (error == "Author not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("DELETE_FAILED", error ?? "Failed to delete author"));
        }

        return NoContent();
    }
}
