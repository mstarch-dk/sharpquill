namespace SharpQuill.Core.Likes;

public interface IArticleLikeRepository
{
    /// <summary>True if the article is publicly visible (published, not a draft or archived).</summary>
    Task<bool> ArticleExistsAsync(int articleId);
    Task<int> GetCountAsync(int articleId);
    Task<bool> HasVisitorLikedAsync(int articleId, string visitorId);
    Task<bool> ToggleLikeAsync(int articleId, string visitorId, string? ipAddress);
}
