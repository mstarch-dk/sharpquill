using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Auth.DTOs;
using SharpQuill.Application.Auth.Services;
using SharpQuill.Core.Authors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SharpQuill.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthorRepository _authorRepository;
    private readonly ILogger<AuthController> _logger;
    private const string RefreshTokenCookieName = "refreshToken";
    private const string CertFingerprintHeader = "X-Client-Cert-Fingerprint";

    public AuthController(IAuthService authService, IAuthorRepository authorRepository, ILogger<AuthController> logger)
    {
        _authService = authService;
        _authorRepository = authorRepository;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (response, refreshToken, error) = await _authService.LoginAsync(request);

        if (response == null)
        {
            return Unauthorized(new ApiErrorResponse("INVALID_CREDENTIALS", error ?? "Invalid email or password"));
        }

        SetRefreshTokenCookie(refreshToken!);
        return Ok(new ApiResponse<LoginResponseDto>(response));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponseDto>), StatusCodes.Status200OK)]
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
            return Unauthorized(new ApiErrorResponse("INVALID_TOKEN", error ?? "Invalid refresh token"));
        }

        SetRefreshTokenCookie(newRefreshToken!);
        return Ok(new ApiResponse<RefreshTokenResponseDto>(response));
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
    [Authorize(Policy = AuthPolicies.AdminUser)]
    [ProducesResponseType(typeof(ApiResponse<AuthorProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var authorId = User.GetAuthorId();
        var profile = await _authService.GetProfileAsync(authorId);

        if (profile == null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Author not found"));
        }

        return Ok(new ApiResponse<AuthorProfileDto>(profile));
    }

    [HttpPost("cert-login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CertLogin()
    {
        var fingerprint = Request.Headers[CertFingerprintHeader].FirstOrDefault();
        var truncated = string.IsNullOrEmpty(fingerprint) ? "(empty)" : fingerprint[..Math.Min(8, fingerprint.Length)] + "...";
        _logger.LogInformation("CertLogin attempt - fingerprint header present: {HasFingerprint}, value: '{Fingerprint}'",
            !string.IsNullOrEmpty(fingerprint), truncated);

        if (string.IsNullOrEmpty(fingerprint))
        {
            return Unauthorized(new ApiErrorResponse("NO_CERTIFICATE", "No client certificate fingerprint provided"));
        }

        var (response, refreshToken, error) = await _authService.CertLoginAsync(fingerprint);

        if (response == null)
        {
            _logger.LogWarning("CertLogin failed - fingerprint not recognized: '{Fingerprint}'", truncated);
            return Unauthorized(new ApiErrorResponse("INVALID_CERTIFICATE", error ?? "Certificate not recognized"));
        }

        SetRefreshTokenCookie(refreshToken!);
        return Ok(new ApiResponse<LoginResponseDto>(response));
    }

    [HttpPost("register-cert")]
    [Authorize(Policy = AuthPolicies.AdminUser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterCert()
    {
        var fingerprint = Request.Headers[CertFingerprintHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(fingerprint))
        {
            return BadRequest(new ApiErrorResponse("NO_CERTIFICATE", "No client certificate fingerprint provided"));
        }

        var authorId = User.GetAuthorId();
        var updated = await _authorRepository.UpdateCertificateThumbprintAsync(authorId, fingerprint);

        if (!updated)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Author not found"));
        }

        return Ok(new ApiResponse<object>(new { registered = true }));
    }

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
