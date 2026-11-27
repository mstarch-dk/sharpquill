using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SharpQuill.Application.Auth.DTOs;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Auth;
using Microsoft.IdentityModel.Tokens;

namespace SharpQuill.Application.Auth.Services;

public class AuthService : IAuthService
{
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), workFactor: 12);

    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IAuthorRepository _authorRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAuthorRepository authorRepository,
        IRefreshTokenRepository refreshTokenRepository,
        JwtSettings jwtSettings)
    {
        _authorRepository = authorRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
    }

    public async Task<(LoginResponseDto? Response, string? RefreshToken, string? Error)> LoginAsync(LoginRequestDto request)
    {
        var author = await _authorRepository.GetByEmailAsync(request.Email);
        if (author == null)
        {
            // Spend the same time as a real verification so response time does not reveal which emails exist
            BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
            return (null, null, "Invalid email or password");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, author.PasswordHash))
        {
            return (null, null, "Invalid email or password");
        }

        var accessToken = GenerateAccessToken(author);
        var refreshToken = await CreateRefreshTokenAsync(author.Id);

        var response = new LoginResponseDto(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            MapToAuthorProfile(author)
        );

        return (response, refreshToken.Token, null);
    }

    public async Task<(LoginResponseDto? Response, string? RefreshToken, string? Error)> CertLoginAsync(string certificateThumbprint)
    {
        var author = await _authorRepository.GetByCertificateThumbprintAsync(certificateThumbprint);
        if (author == null)
        {
            return (null, null, "No author found for this certificate");
        }

        var accessToken = GenerateAccessToken(author);
        var refreshToken = await CreateRefreshTokenAsync(author.Id);

        var response = new LoginResponseDto(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            MapToAuthorProfile(author)
        );

        return (response, refreshToken.Token, null);
    }

    public async Task<(RefreshTokenResponseDto? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (existingToken == null)
        {
            return (null, null, "Invalid refresh token");
        }

        if (existingToken.IsRevoked)
        {
            // A rotated token replayed after the grace period (which absorbs concurrent refreshes from
            // several tabs) means a copy of it exists elsewhere: end every session of this author.
            if (existingToken.ReplacedByToken != null && existingToken.RevokedAt < DateTime.UtcNow - ReuseGracePeriod)
            {
                await _refreshTokenRepository.RevokeAllForAuthorAsync(existingToken.AuthorId);
            }
            return (null, null, "Refresh token is no longer valid");
        }

        if (existingToken.IsExpired)
        {
            return (null, null, "Refresh token is no longer valid");
        }

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(existingToken.Author);
        var newRefreshToken = BuildRefreshToken(existingToken.AuthorId);

        // Revoke the old token first; the conditional update means only one concurrent refresh can win
        if (!await _refreshTokenRepository.RevokeAsync(existingToken.Id, newRefreshToken.Token))
        {
            return (null, null, "Refresh token is no longer valid");
        }
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        var response = new RefreshTokenResponseDto(
            newAccessToken.Token,
            newAccessToken.ExpiresInSeconds
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

    public async Task<AuthorProfileDto?> GetProfileAsync(int authorId)
    {
        var author = await _authorRepository.GetByIdAsync(authorId);
        return author == null ? null : MapToAuthorProfile(author);
    }

    private (string Token, int ExpiresInSeconds) GenerateAccessToken(Author author)
    {
        var expiresInSeconds = _jwtSettings.AccessTokenExpirationMinutes * 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, author.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, author.Email),
            new Claim(JwtRegisteredClaimNames.Name, author.Name),
            new Claim("role", author.Role),
            new Claim("user_type", "admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.AdminAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInSeconds);
    }

    private RefreshToken BuildRefreshToken(int authorId)
    {
        return new RefreshToken
        {
            Token = GenerateRefreshToken(),
            AuthorId = authorId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int authorId)
    {
        return await _refreshTokenRepository.CreateAsync(BuildRefreshToken(authorId));
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static AuthorProfileDto MapToAuthorProfile(Author author)
    {
        return new AuthorProfileDto(
            author.Id,
            author.Email,
            author.Name,
            author.Role,
            author.Bio,
            author.AvatarUrl
        );
    }
}
