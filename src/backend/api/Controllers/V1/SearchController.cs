using SharpQuill.Api.Infrastructure.Auth;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

/// <summary>Public site search over published articles (Postgres full-text search).</summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IArticleService _articleService;

    public SearchController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    /// <summary>
    /// Searches titles, excerpts and body text. <paramref name="q"/> uses web search syntax ("exact phrase",
    /// or, -exclude); any input is accepted and a blank query returns no results. Members-only articles are
    /// included for signed-in readers only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.SearchAsync(q, User.IsWebsiteUser(), pagination);
        return Ok(new ApiResponse<IReadOnlyList<SearchResultDto>>(result.Data, result.Meta));
    }

    /// <summary>
    /// Search-as-you-type suggestions: published articles whose title has a word starting with each word of
    /// <paramref name="q"/> (members-only ones for signed-in readers only), followed by matching tags, categories and
    /// series. Answered from the in-memory suggestion index, without a database query; fewer than 2 letters return
    /// nothing.
    /// </summary>
    [HttpGet("suggest")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SearchSuggestionDto>>), StatusCodes.Status200OK)]
    public IActionResult Suggest([FromServices] ISearchSuggestionService suggestions, [FromQuery] string? q)
    {
        return Ok(new ApiResponse<IReadOnlyList<SearchSuggestionDto>>(suggestions.SuggestPublic(q, User.IsWebsiteUser())));
    }
}
