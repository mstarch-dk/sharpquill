namespace SharpQuill.Application.Comments.DTOs;

public record CommentCreateDto(
    string AuthorName,
    string AuthorEmail,
    string Content,
    int? ParentCommentId,
    string? Honeypot
);
