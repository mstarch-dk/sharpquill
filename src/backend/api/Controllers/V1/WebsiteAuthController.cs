using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Settings.Services;
using SharpQuill.Application.WebsiteUsers.DTOs;
using SharpQuill.Application.WebsiteUsers.Services;
using SharpQuill.Core.WebsiteUsers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class WebsiteAuthController : ControllerBase
{
    private readonly IWebsiteAuthService _authService;
    private readonly ISiteSettingsService _settingsService;
    private const string RefreshTokenCookieName = "websiteRefreshToken";

    public WebsiteAuthController(IWebsiteAuthService authService, ISiteSettingsService settingsService)
    {
        _authService = authService;
        _settingsService = settingsService;
    }

    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<WebsiteUserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Signup([FromBody] WebsiteUserSignupDto dto)
    {
        if (!await IsLoginEnabledAsync())
        {
            return LoginDisabled();
        }

        // An already-registered email gets the same response as a new one, so signup cannot be used to
        // find out who is registered. Nothing about the existing account is changed or revealed.
        await _authService.SignupAsync(dto);

        var accepted = new WebsiteUserDto(0, dto.Name, dto.Email, WebsiteUserStatus.Pending, DateTime.UtcNow, null);
        return StatusCode(201, new ApiResponse<WebsiteUserDto>(accepted));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<WebsiteLoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] WebsiteLoginRequestDto request)
    {
        // Checked before redeeming so a token isn't consumed while sign-in is switched off
        if (!await IsLoginEnabledAsync())
        {
            return LoginDisabled();
        }

        var (response, refreshToken, error) = await _authService.LoginAsync(request.Token);
        if (response == null)
        {
            return Unauthorized(new ApiErrorResponse("INVALID_TOKEN", error!));
        }

        SetRefreshTokenCookie(refreshToken!);
        return Ok(new ApiResponse<WebsiteLoginResponseDto>(response));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(typeof(ApiResponse<WebsiteLoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ApiErrorResponse("INVALID_TOKEN", "Refresh token is required"));
        }

        var (response, newRefreshToken, error) = await _authService.RefreshAsync(refreshToken);
        if (response == null)
        {
            return Unauthorized(new ApiErrorResponse("INVALID_TOKEN", error!));
        }

        SetRefreshTokenCookie(newRefreshToken!);
        return Ok(new ApiResponse<WebsiteLoginResponseDto>(response));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    [HttpGet("profile")]
    [Authorize(Policy = AuthPolicies.WebsiteUser)]
    [ProducesResponseType(typeof(ApiResponse<WebsiteUserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetWebsiteUserId();
        var profile = await _authService.GetProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "User not found"));
        }

        return Ok(new ApiResponse<WebsiteUserProfileDto>(profile));
    }

    private async Task<bool> IsLoginEnabledAsync() => (await _settingsService.GetAllAsync()).LoginEnabled;

    private ObjectResult LoginDisabled() =>
        StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse("LOGIN_DISABLED", "Sign-in is disabled on this site"));

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }
}
