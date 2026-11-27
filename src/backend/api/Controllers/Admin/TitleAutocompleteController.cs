using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Core.Articles;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/title-autocomplete")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class TitleAutocompleteController : ControllerBase
{
    private readonly ITitleAutocompleteRepository _repository;

    public TitleAutocompleteController(ITitleAutocompleteRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TitleAutocompleteResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q = "", [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new ApiResponse<List<TitleAutocompleteResultDto>>(new List<TitleAutocompleteResultDto>()));

        var clampedLimit = Math.Clamp(limit, 1, 20);
        var results = await _repository.SearchAsync(q, clampedLimit);

        var dtos = results.Select(r => new TitleAutocompleteResultDto(
            r.Id,
            r.Title,
            r.Slug,
            r.ContentType == ContentType.Article ? "article" : "page",
            r.Url
        )).ToList();

        return Ok(new ApiResponse<List<TitleAutocompleteResultDto>>(dtos));
    }
}

public record TitleAutocompleteResultDto(int Id, string Title, string Slug, string ContentType, string Url);
