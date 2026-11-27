namespace SharpQuill.Application.TweetPreviews.DTOs;

public record TweetPreviewDto(
    string? AuthorName,
    string? AuthorHandle,
    string? TweetText,
    string? TweetDate,
    string? ProfileUrl
);
