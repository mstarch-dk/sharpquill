using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Settings.DTOs;
using SharpQuill.Application.Settings.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class SettingsController : ControllerBase
{
    private readonly ISiteSettingsService _settingsService;

    public SettingsController(ISiteSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SiteSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetAllAsync();
        return Ok(new ApiResponse<SiteSettingsDto>(settings));
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.AdminRole)]
    [ProducesResponseType(typeof(ApiResponse<SiteSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] SiteSettingsUpdateDto dto)
    {
        var settings = await _settingsService.UpdateAsync(dto);
        return Ok(new ApiResponse<SiteSettingsDto>(settings));
    }
}
