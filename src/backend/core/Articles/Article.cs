using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;

namespace SharpQuill.Core.Articles;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ArticleStatus Status { get; set; }
    public ContentType ContentType { get; set; }

    public int? PublishedVersionId { get; set; }
    public ArticleContent? PublishedVersion { get; set; }

    public int? AuthorId { get; set; }
    public Author? Author { get; set; }
    public string? AuthorName { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? FeaturedImageId { get; set; }
    public MediaItem? FeaturedImage { get; set; }

    public bool CommentsEnabled { get; set; } = true;
    public bool RequiresSignIn { get; set; }

    public int? SeriesId { get; set; }
    public int? SeriesOrder { get; set; }
    public ArticleSeries? Series { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    public ICollection<ArticleContent> Versions { get; set; } = new List<ArticleContent>();

    // Transport properties - hydrated by repositories, not mapped to DB columns
    public string? Content { get; set; }
    public string? Layout { get; set; }
    public string? Excerpt { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? PageStyle { get; set; }
    public string? FeaturedImageSize { get; set; }
    public int? CurrentVersionNumber { get; set; }
    public int TotalVersions { get; set; }
}
