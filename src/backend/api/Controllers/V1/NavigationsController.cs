using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Application.Navigations.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class NavigationsController : ControllerBase
{
    private readonly INavigationService _navigationService;

    public NavigationsController(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<NavigationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNavigationBySlug(string slug)
    {
        var navigation = await _navigationService.GetBySlugAsync(slug);
        if (navigation == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Navigation with slug '{slug}' not found"));
        }
        return Ok(new ApiResponse<NavigationDto>(navigation));
    }
}
