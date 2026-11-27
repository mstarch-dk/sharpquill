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
[Route("api/admin/articles")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class ArticlesController : ControllerBase
{
    private readonly IAdminArticleService _articleService;
    private readonly IAuthorizationService _authorizationService;

    public ArticlesController(IAdminArticleService articleService, IAuthorizationService authorizationService)
    {
        _articleService = articleService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Checks the article ownership rule (<see cref="ArticleOwnershipHandler"/>). Returns a result to
    /// short-circuit with, or null when the caller may proceed (or the article does not exist).
    /// </summary>
    private async Task<IActionResult?> EnsureCanModifyAsync(int articleId)
    {
        var existing = await _articleService.GetByIdAsync(articleId);
        if (existing == null)
        {
            return null;
        }

        var authorization = await _authorizationService.AuthorizeAsync(User, existing, ModifyArticleRequirement.Instance);
        if (!authorization.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiErrorResponse("FORBIDDEN", "You can only modify your own articles"));
        }

        return null;
    }

    /// <summary>
    /// Lists articles of every status. Optional filters combine: <paramref name="search"/> (web search syntax over the
    /// latest version, or text in the title), <paramref name="status"/> (draft, published, scheduled, archived),
    /// <paramref name="categoryId"/>, <paramref name="authorId"/>; <paramref name="sort"/> is relevance, newest,
    /// oldest, updated or title (default: relevance with a search, otherwise newest).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleAdminDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetArticles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? authorId = null,
        [FromQuery] string? sort = null)
    {
        if (!TryParseName<ArticleAdminStatusFilter>(status, out var statusFilter))
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR",
                "Invalid status. Use draft, published, scheduled or archived."));
        }

        if (!TryParseName<ArticleAdminSort>(sort, out var sortOrder))
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR",
                "Invalid sort. Use relevance, newest, oldest, updated or title."));
        }

        var filter = new ArticleAdminFilter
        {
            Search = search,
            Status = statusFilter,
            CategoryId = categoryId,
            AuthorId = authorId,
            Sort = sortOrder
        };

        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _articleService.GetAllAsync(pagination, ContentType.Article, filter);
        return Ok(new ApiResponse<IReadOnlyList<ArticleAdminDto>>(result.Data, result.Meta));
    }

    /// <summary>
    /// Search-as-you-type suggestions: articles in every status whose title has a word starting with each word of
    /// <paramref name="q"/>. Answered from the in-memory suggestion index; fewer than 2 letters return nothing.
    /// </summary>
    [HttpGet("suggest")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleSuggestionDto>>), StatusCodes.Status200OK)]
    public IActionResult Suggest(
        [FromServices] ISearchSuggestionService suggestions, [FromQuery] string? q = null, [FromQuery] int limit = 8)
    {
        return Ok(new ApiResponse<IReadOnlyList<ArticleSuggestionDto>>(suggestions.SuggestAdmin(q, limit)));
    }

    /// <summary>Parses an enum by member name only (case-insensitive); blank means not set. Numbers are rejected.</summary>
    private static bool TryParseName<TEnum>(string? value, out TEnum? result) where TEnum : struct, Enum
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Enum.GetNames<TEnum>().FirstOrDefault(n => n.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
            is not { } name)
        {
            return false;
        }

        result = Enum.Parse<TEnum>(name);
        return true;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Article with id '{id}' not found"));
        }
        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateArticle([FromBody] ArticleCreateDto dto)
    {
        var authorId = User.GetAuthorId();
        var (article, error) = await _articleService.CreateAsync(dto, authorId);

        if (article == null)
        {
            return BadRequest(new ApiErrorResponse("CREATION_FAILED", error ?? "Failed to create article"));
        }

        return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] ArticleUpdateDto dto)
    {
        var denied = await EnsureCanModifyAsync(id);
        if (denied != null) return denied;

        var (article, error) = await _articleService.UpdateAsync(id, dto);

        if (article == null)
        {
            if (error == "Article not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("UPDATE_FAILED", error ?? "Failed to update article"));
        }

        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var denied = await EnsureCanModifyAsync(id);
        if (denied != null) return denied;

        var deleted = await _articleService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Article with id '{id}' not found"));
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
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Article with id '{id}' not found"));
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
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Article with id '{id}' not found"));
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
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Article or version not found"));
        }
        return Ok(new ApiResponse<ArticleVersionDetailDto>(version));
    }

    [HttpPut("{id:int}/versions/{versionId:int}/publish")]
    [ProducesResponseType(typeof(ApiResponse<ArticleAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishVersion(int id, int versionId)
    {
        var denied = await EnsureCanModifyAsync(id);
        if (denied != null) return denied;

        var (article, error) = await _articleService.PublishVersionAsync(id, versionId);
        if (article == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error ?? "Article or version not found"));
        }
        return Ok(new ApiResponse<ArticleAdminDto>(article));
    }
}
