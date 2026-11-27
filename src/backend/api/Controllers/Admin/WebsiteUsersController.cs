using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.WebsiteUsers.DTOs;
using SharpQuill.Application.WebsiteUsers.Services;
using SharpQuill.Core.Common;
using SharpQuill.Core.WebsiteUsers;
using Microsoft.AspNetCore.Mvc;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/website-users")]
[Authorize(Policy = AuthPolicies.AdminRole)]
public class WebsiteUsersController : ControllerBase
{
    private readonly IAdminWebsiteUserService _userService;

    public WebsiteUsersController(IAdminWebsiteUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WebsiteUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WebsiteUserStatus? status = null)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _userService.GetAllAsync(pagination, status);
        return Ok(new ApiResponse<IReadOnlyList<WebsiteUserDto>>(result.Data, result.Meta));
    }

    [HttpGet("counts")]
    [ProducesResponseType(typeof(ApiResponse<WebsiteUserCountsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCounts()
    {
        var counts = await _userService.GetCountsAsync();
        return Ok(new ApiResponse<WebsiteUserCountsDto>(counts));
    }

    [HttpPut("{id:int}/moderate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Moderate(int id, [FromBody] WebsiteUserModerationDto dto)
    {
        var (success, error) = await _userService.ModerateAsync(id, dto.Status);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", error!));
        }
        return NoContent();
    }

    [HttpPost("{id:int}/login-token")]
    [ProducesResponseType(typeof(ApiResponse<WebsiteLoginTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateLoginToken(int id)
    {
        var (token, error) = await _userService.GenerateLoginTokenAsync(id);
        if (token == null)
        {
            if (error == "User not found")
            {
                return NotFound(new ApiErrorResponse("NOT_FOUND", error));
            }
            return BadRequest(new ApiErrorResponse("INVALID_STATUS", error!));
        }

        return Ok(new ApiResponse<WebsiteLoginTokenDto>(token));
    }
}
