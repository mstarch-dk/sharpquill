using SharpQuill.Application.Auth.DTOs;

namespace SharpQuill.Application.Auth.Services;

public interface IAuthService
{
    Task<(LoginResponseDto? Response, string? RefreshToken, string? Error)> LoginAsync(LoginRequestDto request);
    Task<(LoginResponseDto? Response, string? RefreshToken, string? Error)> CertLoginAsync(string certificateThumbprint);
    Task<(RefreshTokenResponseDto? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<AuthorProfileDto?> GetProfileAsync(int authorId);
}
