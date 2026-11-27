namespace SharpQuill.Core.WebsiteUsers;

public interface IWebsiteRefreshTokenRepository
{
    Task<WebsiteRefreshToken?> GetByTokenAsync(string token);
    Task<WebsiteRefreshToken> CreateAsync(WebsiteRefreshToken refreshToken);
    /// <summary>Revokes the token if it is still active. Returns false if it was already revoked (lost a race).</summary>
    Task<bool> RevokeAsync(int id, string? replacedByToken = null);
    Task RevokeAllForUserAsync(int websiteUserId);
}
