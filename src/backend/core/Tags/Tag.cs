using SharpQuill.Core.Articles;

namespace SharpQuill.Core.Tags;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}
