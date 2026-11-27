using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Application.Navigations.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/navigations")]
[Authorize(Policy = AuthPolicies.AdminUser)]
public class NavigationsController : ControllerBase
{
    private readonly IAdminNavigationService _navigationService;

    public NavigationsController(IAdminNavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NavigationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNavigations()
    {
        var navigations = await _navigationService.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<NavigationSummaryDto>>(navigations));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<NavigationAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNavigationById(int id)
    {
        var navigation = await _navigationService.GetByIdAsync(id);
        if (navigation == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Navigation with id '{id}' not found"));
        }
        return Ok(new ApiResponse<NavigationAdminDto>(navigation));
    }

    [HttpPut("{id:int}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveItems(int id, [FromBody] NavigationItemsBulkSaveDto dto)
    {
        var (success, error) = await _navigationService.SaveItemsAsync(id, dto);

        if (!success)
        {
            if (error == "Navigation not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("SAVE_FAILED", error ?? "Failed to save navigation items"));
        }

        return Ok(new { success = true });
    }
}
