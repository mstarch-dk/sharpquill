using SharpQuill.Application.Likes.DTOs;

namespace SharpQuill.Application.Likes.Services;

public interface ILikeService
{
    Task<LikeStatusDto> GetStatusAsync(int articleId, string? visitorId);
    /// <summary>Toggles the visitor's like. Returns null if the article is not publicly visible.</summary>
    Task<LikeStatusDto?> ToggleAsync(int articleId, string visitorId, string? ipAddress);
}
