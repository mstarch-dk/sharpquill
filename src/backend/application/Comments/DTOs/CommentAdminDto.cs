using SharpQuill.Core.Comments;

namespace SharpQuill.Application.Comments.DTOs;

public record CommentAdminDto(
    int Id,
    int ArticleId,
    string? ArticleTitle,
    string AuthorName,
    string AuthorEmail,
    string Content,
    CommentStatus Status,
    string? IpAddress,
    int? ParentCommentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
