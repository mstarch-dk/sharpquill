using SharpQuill.Api.Infrastructure.Auth;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArticles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.GetPublishedAsync(pagination);
        return Ok(new ApiResponse<IReadOnlyList<ArticleListDto>>(result.Data, result.Meta));
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(string slug)
    {
        var article = await _articleService.GetBySlugAsync(slug);
        if (article == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Article with slug '{slug}' not found"));
        }

        if (article.RequiresSignIn && !User.IsWebsiteUser())
        {
            return Unauthorized(new ApiErrorResponse("SIGN_IN_REQUIRED", "This article requires sign-in to view"));
        }

        return Ok(new ApiResponse<ArticleDto>(article));
    }

    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedArticles([FromQuery] int count = 5)
    {
        var validCount = Math.Clamp(count, 1, 10);
        var articles = await _articleService.GetFeaturedAsync(validCount);
        return Ok(new ApiResponse<IReadOnlyList<ArticleListDto>>(articles));
    }
}
