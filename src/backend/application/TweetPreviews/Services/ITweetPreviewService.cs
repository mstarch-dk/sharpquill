using SharpQuill.Application.TweetPreviews.DTOs;

namespace SharpQuill.Application.TweetPreviews.Services;

public interface ITweetPreviewService
{
    Task<TweetPreviewDto> FetchPreviewAsync(string tweetUrl);
}
