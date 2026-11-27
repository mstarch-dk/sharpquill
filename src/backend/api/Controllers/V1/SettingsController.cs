using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Settings.DTOs;
using SharpQuill.Application.Settings.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISiteSettingsService _settingsService;

    public SettingsController(ISiteSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PublicSiteSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        // Public allow-list only: never the full settings (contact_email is the private contact form recipient)
        var settings = await _settingsService.GetAllAsync();
        return Ok(new ApiResponse<PublicSiteSettingsDto>(PublicSiteSettingsDto.From(settings)));
    }
}
