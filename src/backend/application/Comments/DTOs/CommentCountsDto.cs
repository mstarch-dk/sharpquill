namespace SharpQuill.Application.Comments.DTOs;

public record CommentCountsDto(
    int Pending,
    int Approved,
    int Rejected
);
