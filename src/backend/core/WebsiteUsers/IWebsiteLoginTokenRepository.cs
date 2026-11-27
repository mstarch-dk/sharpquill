namespace SharpQuill.Core.WebsiteUsers;

public interface IWebsiteLoginTokenRepository
{
    Task<WebsiteLoginToken?> GetByTokenAsync(string token);
    Task<WebsiteLoginToken> CreateAsync(WebsiteLoginToken loginToken);
    /// <summary>Marks the token used if it is still unused. Returns false if another request used it first.</summary>
    Task<bool> MarkUsedAsync(int id);
}
