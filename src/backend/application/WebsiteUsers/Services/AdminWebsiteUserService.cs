using System.Security.Cryptography;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.WebsiteUsers.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Services;

public class AdminWebsiteUserService : IAdminWebsiteUserService
{
    private readonly IAdminWebsiteUserRepository _userRepository;
    private readonly IWebsiteLoginTokenRepository _loginTokenRepository;
    private readonly IWebsiteRefreshTokenRepository _refreshTokenRepository;

    public AdminWebsiteUserService(
        IAdminWebsiteUserRepository userRepository,
        IWebsiteLoginTokenRepository loginTokenRepository,
        IWebsiteRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _loginTokenRepository = loginTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<PagedResultDto<WebsiteUserDto>> GetAllAsync(PaginationParams pagination, WebsiteUserStatus? status = null)
    {
        var (users, totalCount) = await _userRepository.GetAllAsync(pagination, status);

        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = users.Select(MapToDto).ToList();
        var meta = new PaginationMeta(page, pageSize, totalCount, totalPages);

        return new PagedResultDto<WebsiteUserDto>(data, meta);
    }

    public async Task<(bool Success, string? Error)> ModerateAsync(int id, WebsiteUserStatus status)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return (false, "User not found");
        }

        await _userRepository.UpdateStatusAsync(id, status);

        if (status != WebsiteUserStatus.Approved)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(id);
        }

        return (true, null);
    }

    public async Task<WebsiteUserCountsDto> GetCountsAsync()
    {
        var (pending, approved, rejected) = await _userRepository.GetCountsByStatusAsync();
        return new WebsiteUserCountsDto(pending, approved, rejected);
    }

    public async Task<(WebsiteLoginTokenDto? Token, string? Error)> GenerateLoginTokenAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (null, "User not found");
        }

        if (user.Status != WebsiteUserStatus.Approved)
        {
            return (null, "User must be approved before generating a login token");
        }

        var tokenString = GenerateHexToken(64);
        var loginToken = new WebsiteLoginToken
        {
            WebsiteUserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _loginTokenRepository.CreateAsync(loginToken);

        var dto = new WebsiteLoginTokenDto(tokenString, loginToken.ExpiresAt);
        return (dto, null);
    }

    private static string GenerateHexToken(int byteLength)
    {
        var randomBytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToHexStringLower(randomBytes);
    }

    private static WebsiteUserDto MapToDto(WebsiteUser user)
    {
        return new WebsiteUserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Status,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
