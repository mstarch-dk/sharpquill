using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Application.Categories.Services;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IArticleService _articleService;

    public CategoriesController(ICategoryService categoryService, IArticleService articleService)
    {
        _categoryService = categoryService;
        _articleService = articleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<CategoryDto>>(categories));
    }

    [HttpGet("{slug}/articles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticlesByCategory(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var category = await _categoryService.GetBySlugAsync(slug);
        if (category == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Category with slug '{slug}' not found"));
        }

        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.GetByCategorySlugAsync(slug, pagination);
        return Ok(new ApiResponse<IReadOnlyList<ArticleListDto>>(result.Data, result.Meta));
    }
}
