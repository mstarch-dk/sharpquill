namespace SharpQuill.Core.Likes;

public class ArticleLike
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string VisitorId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
