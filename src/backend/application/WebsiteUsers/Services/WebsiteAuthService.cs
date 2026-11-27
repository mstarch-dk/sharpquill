using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SharpQuill.Application.Auth;
using SharpQuill.Application.WebsiteUsers.DTOs;
using SharpQuill.Core.WebsiteUsers;
using Microsoft.IdentityModel.Tokens;

namespace SharpQuill.Application.WebsiteUsers.Services;

public class WebsiteAuthService : IWebsiteAuthService
{
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IWebsiteUserRepository _userRepository;
    private readonly IWebsiteLoginTokenRepository _loginTokenRepository;
    private readonly IWebsiteRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;

    public WebsiteAuthService(
        IWebsiteUserRepository userRepository,
        IWebsiteLoginTokenRepository loginTokenRepository,
        IWebsiteRefreshTokenRepository refreshTokenRepository,
        JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _loginTokenRepository = loginTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
    }

    public async Task<(WebsiteUserDto? User, string? Error)> SignupAsync(WebsiteUserSignupDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            return (null, "A user with this email already exists");
        }

        var user = new WebsiteUser
        {
            Name = dto.Name,
            Email = dto.Email,
            Status = WebsiteUserStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        user = await _userRepository.CreateAsync(user);

        var userDto = new WebsiteUserDto(
            user.Id, user.Name, user.Email, user.Status, user.CreatedAt, user.UpdatedAt
        );

        return (userDto, null);
    }

    public async Task<(WebsiteLoginResponseDto? Response, string? RefreshToken, string? Error)> LoginAsync(string token)
    {
        var loginToken = await _loginTokenRepository.GetByTokenAsync(token);
        if (loginToken == null)
        {
            return (null, null, "Invalid login token");
        }

        if (!loginToken.IsValid)
        {
            return (null, null, "Login token has expired or already been used");
        }

        if (loginToken.WebsiteUser.Status != WebsiteUserStatus.Approved)
        {
            return (null, null, "User account is not approved");
        }

        // Mark token as used; the conditional update lets only one concurrent redemption succeed
        if (!await _loginTokenRepository.MarkUsedAsync(loginToken.Id))
        {
            return (null, null, "Login token has expired or already been used");
        }

        // Generate JWT and refresh token
        var accessToken = GenerateAccessToken(loginToken.WebsiteUser);
        var refreshToken = await CreateRefreshTokenAsync(loginToken.WebsiteUserId);

        var profile = new WebsiteUserProfileDto(
            loginToken.WebsiteUser.Id,
            loginToken.WebsiteUser.Name,
            loginToken.WebsiteUser.Email
        );

        var response = new WebsiteLoginResponseDto(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            profile
        );

        return (response, refreshToken.Token, null);
    }

    public async Task<(WebsiteLoginResponseDto? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (existingToken == null)
        {
            return (null, null, "Invalid refresh token");
        }

        if (existingToken.IsRevoked)
        {
            // See AuthService.RefreshAsync: replay of a rotated token ends all sessions of this user
            if (existingToken.ReplacedByToken != null && existingToken.RevokedAt < DateTime.UtcNow - ReuseGracePeriod)
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.WebsiteUserId);
            }
            return (null, null, "Refresh token is no longer valid");
        }

        if (existingToken.IsExpired)
        {
            return (null, null, "Refresh token is no longer valid");
        }

        // A user rejected or reset to pending after signing in must not keep refreshing
        if (existingToken.WebsiteUser.Status != WebsiteUserStatus.Approved)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.WebsiteUserId);
            return (null, null, "User account is not approved");
        }

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(existingToken.WebsiteUser);
        var newRefreshToken = BuildRefreshToken(existingToken.WebsiteUserId);

        if (!await _refreshTokenRepository.RevokeAsync(existingToken.Id, newRefreshToken.Token))
        {
            return (null, null, "Refresh token is no longer valid");
        }
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        var profile = new WebsiteUserProfileDto(
            existingToken.WebsiteUser.Id,
            existingToken.WebsiteUser.Name,
            existingToken.WebsiteUser.Email
        );

        var response = new WebsiteLoginResponseDto(
            newAccessToken.Token,
            newAccessToken.ExpiresInSeconds,
            profile
        );

        return (response, newRefreshToken.Token, null);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (existingToken == null || !existingToken.IsActive)
        {
            return false;
        }

        await _refreshTokenRepository.RevokeAsync(existingToken.Id);
        return true;
    }

    public async Task<WebsiteUserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new WebsiteUserProfileDto(user.Id, user.Name, user.Email);
    }

    private (string Token, int ExpiresInSeconds) GenerateAccessToken(WebsiteUser user)
    {
        var expiresInSeconds = _jwtSettings.AccessTokenExpirationMinutes * 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("user_type", "website"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.WebsiteAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInSeconds);
    }

    private WebsiteRefreshToken BuildRefreshToken(int userId)
    {
        return new WebsiteRefreshToken
        {
            Token = GenerateRefreshToken(),
            WebsiteUserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<WebsiteRefreshToken> CreateRefreshTokenAsync(int userId)
    {
        return await _refreshTokenRepository.CreateAsync(BuildRefreshToken(userId));
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
