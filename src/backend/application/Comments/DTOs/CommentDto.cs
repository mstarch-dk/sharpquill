namespace SharpQuill.Application.Comments.DTOs;

public record CommentDto(
    int Id,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    int? ParentCommentId,
    IReadOnlyList<CommentDto> Replies
);
