using SharpQuill.Api.Infrastructure.Auth;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Core.Articles;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class PagesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public PagesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageBySlug(string slug)
    {
        var page = await _articleService.GetBySlugAsync(slug, ContentType.Page);
        if (page == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Page with slug '{slug}' not found"));
        }

        if (page.RequiresSignIn && !User.IsWebsiteUser())
        {
            return Unauthorized(new ApiErrorResponse("SIGN_IN_REQUIRED", "This page requires sign-in to view"));
        }

        return Ok(new ApiResponse<ArticleDto>(page));
    }
}
