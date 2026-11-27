using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.WebsiteUsers.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Services;

public interface IAdminWebsiteUserService
{
    Task<PagedResultDto<WebsiteUserDto>> GetAllAsync(PaginationParams pagination, WebsiteUserStatus? status = null);
    Task<(bool Success, string? Error)> ModerateAsync(int id, WebsiteUserStatus status);
    Task<WebsiteUserCountsDto> GetCountsAsync();
    Task<(WebsiteLoginTokenDto? Token, string? Error)> GenerateLoginTokenAsync(int userId);
}
