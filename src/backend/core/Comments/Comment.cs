namespace SharpQuill.Core.Comments;

public class Comment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public int? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Comment? Parent { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    // Transport property - article title for admin views
    public string? ArticleTitle { get; set; }
}
