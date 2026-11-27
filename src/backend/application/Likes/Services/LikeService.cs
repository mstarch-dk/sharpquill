using SharpQuill.Application.Likes.DTOs;
using SharpQuill.Core.Likes;

namespace SharpQuill.Application.Likes.Services;

public class LikeService : ILikeService
{
    private readonly IArticleLikeRepository _likeRepository;

    public LikeService(IArticleLikeRepository likeRepository)
    {
        _likeRepository = likeRepository;
    }

    public async Task<LikeStatusDto> GetStatusAsync(int articleId, string? visitorId)
    {
        var count = await _likeRepository.GetCountAsync(articleId);
        var hasLiked = !string.IsNullOrEmpty(visitorId) && await _likeRepository.HasVisitorLikedAsync(articleId, visitorId);
        return new LikeStatusDto(count, hasLiked);
    }

    public async Task<LikeStatusDto?> ToggleAsync(int articleId, string visitorId, string? ipAddress)
    {
        if (!await _likeRepository.ArticleExistsAsync(articleId))
            return null;

        var liked = await _likeRepository.ToggleLikeAsync(articleId, visitorId, ipAddress);
        var count = await _likeRepository.GetCountAsync(articleId);
        return new LikeStatusDto(count, liked);
    }
}
