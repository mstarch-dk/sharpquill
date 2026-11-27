using SharpQuill.Application.WebsiteUsers.DTOs;

namespace SharpQuill.Application.WebsiteUsers.Services;

public interface IWebsiteAuthService
{
    Task<(WebsiteUserDto? User, string? Error)> SignupAsync(WebsiteUserSignupDto dto);
    Task<(WebsiteLoginResponseDto? Response, string? RefreshToken, string? Error)> LoginAsync(string token);
    Task<(WebsiteLoginResponseDto? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<WebsiteUserProfileDto?> GetProfileAsync(int userId);
}
