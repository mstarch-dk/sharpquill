namespace SharpQuill.Core.WebsiteUsers;

public interface IWebsiteUserRepository
{
    Task<WebsiteUser?> GetByEmailAsync(string email);
    Task<WebsiteUser?> GetByIdAsync(int id);
    Task<WebsiteUser> CreateAsync(WebsiteUser user);
}
