using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/pages")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class PagesController : ControllerBase
{
    private readonly IAdminArticleService _articleService;

    public PagesController(IAdminArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleAdminDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.GetAllAsync(pagination, ContentType.Page);
        return Ok(new ApiResponse<IReadOnlyList<ArticleAdminDto>>(result.Data, result.Meta));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageById(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article == null || article.ContentType != ContentType.Page)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Page with id '{id}' not found"));
        }
        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePage([FromBody] ArticleCreateDto dto)
    {
        // Override content type to Page, no author for pages
        var pageDto = dto with { ContentType = ContentType.Page };
        var (article, error) = await _articleService.CreateAsync(pageDto, null);

        if (article == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create page"));
        }

        return CreatedAtAction(nameof(GetPageById), new { id = article.Id }, new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePage(int id, [FromBody] ArticleUpdateDto dto)
    {
        var pageDto = dto with { ContentType = ContentType.Page };
        var (article, error) = await _articleService.UpdateAsync(id, pageDto);

        if (article == null)
        {
            if (error == "Item not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update page"));
        }

        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePage(int id)
    {
        var deleted = await _articleService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Page with id '{id}' not found"));
        }
        return NoContent();
    }

    [HttpGet("{id:int}/preview")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(int id)
    {
        var article = await _articleService.GetPreviewAsync(id);
        if (article == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Page with id '{id}' not found"));
        }
        return Ok(new ApiResponse<ArticleDto>(article));
    }

    [HttpGet("{id:int}/versions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleVersionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(int id)
    {
        var versions = await _articleService.GetVersionsAsync(id);
        if (versions == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Page with id '{id}' not found"));
        }
        return Ok(new ApiResponse<IReadOnlyList<ArticleVersionDto>>(versions));
    }

    [HttpGet("{id:int}/versions/{versionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleVersionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(int id, int versionId)
    {
        var version = await _articleService.GetVersionAsync(id, versionId);
        if (version == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Page or version not found"));
        }
        return Ok(new ApiResponse<ArticleVersionDetailDto>(version));
    }

    [HttpPut("{id:int}/versions/{versionId:int}/publish")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishVersion(int id, int versionId)
    {
        var (article, error) = await _articleService.PublishVersionAsync(id, versionId);
        if (article == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error ?? "Page or version not found"));
        }
        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }
}
