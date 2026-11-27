using SharpQuill.Application.Comments.DTOs;

namespace SharpQuill.Application.Comments.Services;

public interface ICommentService
{
    Task<bool> RequiresSignInAsync(int articleId);
    Task<IReadOnlyList<CommentDto>> GetApprovedByArticleIdAsync(int articleId);
    Task<(CommentDto? Comment, string? Error)> CreateAsync(int articleId, CommentCreateDto dto, string? ipAddress);
}
