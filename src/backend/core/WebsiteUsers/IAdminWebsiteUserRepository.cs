using SharpQuill.Core.Common;

namespace SharpQuill.Core.WebsiteUsers;

public interface IAdminWebsiteUserRepository
{
    Task<(IEnumerable<WebsiteUser> Users, int TotalCount)> GetAllAsync(PaginationParams pagination, WebsiteUserStatus? status = null);
    Task<WebsiteUser?> GetByIdAsync(int id);
    Task UpdateStatusAsync(int id, WebsiteUserStatus status);
    Task<(int Pending, int Approved, int Rejected)> GetCountsByStatusAsync();
}
