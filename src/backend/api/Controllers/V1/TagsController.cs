using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Application.Tags.DTOs;
using SharpQuill.Application.Tags.Services;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IArticleService _articleService;

    public TagsController(ITagService tagService, IArticleService articleService)
    {
        _tagService = tagService;
        _articleService = articleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _tagService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<TagDto>>(tags));
    }

    [HttpGet("{slug}/articles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticlesByTag(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var tag = await _tagService.GetBySlugAsync(slug);
        if (tag == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Tag with slug '{slug}' not found"));
        }

        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.GetByTagSlugAsync(slug, pagination);
        return Ok(new ApiResponse<IReadOnlyList<ArticleListDto>>(result.Data, result.Meta));
    }
}
