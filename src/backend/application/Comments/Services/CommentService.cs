using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Core.Comments;

namespace SharpQuill.Application.Comments.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;

    public CommentService(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public Task<bool> RequiresSignInAsync(int articleId) => _commentRepository.RequiresSignInAsync(articleId);

    public async Task<IReadOnlyList<CommentDto>> GetApprovedByArticleIdAsync(int articleId)
    {
        var comments = (await _commentRepository.GetApprovedByArticleIdAsync(articleId)).ToList();

        // Build comment tree: top-level comments with nested replies
        var topLevel = comments.Where(c => c.ParentCommentId == null).ToList();
        var byParent = comments.Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return topLevel.Select(c => MapToCommentDto(c, byParent)).ToList();
    }

    public async Task<(CommentDto? Comment, string? Error)> CreateAsync(int articleId, CommentCreateDto dto, string? ipAddress)
    {
        // Honeypot check - silently accept but don't store
        if (!string.IsNullOrEmpty(dto.Honeypot))
        {
            // Return a fake success to not reveal the spam check
            return (new CommentDto(0, dto.AuthorName, dto.Content, DateTime.UtcNow, dto.ParentCommentId, []), null);
        }

        // Verify article exists
        if (!await _commentRepository.ArticleExistsAsync(articleId))
        {
            return (null, "Article not found");
        }

        // Verify comments are enabled
        if (!await _commentRepository.CommentsEnabledAsync(articleId))
        {
            return (null, "Comments are disabled for this article");
        }

        // Validate parent comment if provided
        if (dto.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.GetByIdAsync(dto.ParentCommentId.Value);
            if (parent == null)
            {
                return (null, "Parent comment not found");
            }

            // Only allow single-level nesting
            if (parent.ParentCommentId != null)
            {
                return (null, "Replies to replies are not allowed");
            }

            // Parent must be for the same article
            if (parent.ArticleId != articleId)
            {
                return (null, "Parent comment belongs to a different article");
            }
        }

        var comment = new Comment
        {
            ArticleId = articleId,
            ParentCommentId = dto.ParentCommentId,
            AuthorName = dto.AuthorName,
            AuthorEmail = dto.AuthorEmail,
            Content = dto.Content,
            Status = CommentStatus.Pending,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.CreateAsync(comment);
        return (new CommentDto(created.Id, created.AuthorName, created.Content, created.CreatedAt, created.ParentCommentId, []), null);
    }

    private static CommentDto MapToCommentDto(Comment comment, Dictionary<int, List<Comment>> byParent)
    {
        var replies = byParent.TryGetValue(comment.Id, out var childComments)
            ? childComments.Select(r => new CommentDto(r.Id, r.AuthorName, r.Content, r.CreatedAt, r.ParentCommentId, [])).ToList()
            : [];

        return new CommentDto(comment.Id, comment.AuthorName, comment.Content, comment.CreatedAt, comment.ParentCommentId, replies);
    }
}
