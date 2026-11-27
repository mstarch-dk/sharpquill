
namespace SharpQuill.Core.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    /// <summary>Revokes the token if it is still active. Returns false if it was already revoked (lost a race).</summary>
    Task<bool> RevokeAsync(int id, string? replacedByToken = null);
    Task RevokeAllForAuthorAsync(int authorId);
}
