namespace SharpQuill.Core.Articles;

public class ArticleContent
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string? Content { get; set; }
    public string? Excerpt { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int VersionNumber { get; set; }
    public string? Layout { get; set; }
    public string? PageStyle { get; set; }
    public string? FeaturedImageSize { get; set; }
    public DateTime CreatedAt { get; set; }

    public Article Article { get; set; } = null!;
}
