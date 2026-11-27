namespace SharpQuill.Core.Media;

/// <summary>Where a media item is still referenced.</summary>
public static class MediaUsageKind
{
    /// <summary>The featured image of an article or page.</summary>
    public const string FeaturedImage = "featured_image";
    /// <summary>Linked or embedded in the Markdown content of the latest or the published version.</summary>
    public const string Content = "content";
    /// <summary>Used by a layout block of the latest or the published version.</summary>
    public const string Layout = "layout";
    /// <summary>Referenced by a site setting value (e.g. a logo URL).</summary>
    public const string Setting = "setting";
    /// <summary>Used as an author avatar.</summary>
    public const string AuthorAvatar = "author_avatar";
}

/// <param name="Kind">One of <see cref="MediaUsageKind"/>.</param>
/// <param name="ArticleId">The article or page, for featured image, content and layout usages.</param>
/// <param name="Title">The article or page title, the setting key or the author name.</param>
/// <param name="IsPage">True when <paramref name="ArticleId"/> is a page rather than an article.</param>
public record MediaUsage(string Kind, int? ArticleId, string Title, bool IsPage);
