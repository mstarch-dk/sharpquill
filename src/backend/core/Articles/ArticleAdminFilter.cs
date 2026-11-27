namespace SharpQuill.Core.Articles;

/// <summary>
/// Status choices of the admin article list. Published and Scheduled split <see cref="ArticleStatus.Published"/> by
/// whether the publish date has passed, matching what the admin shows.
/// </summary>
public enum ArticleAdminStatusFilter
{
    Draft,
    Published,
    Scheduled,
    Archived
}

/// <summary>Sort orders of the admin article list.</summary>
public enum ArticleAdminSort
{
    /// <summary>Best text match first; without a search text this is <see cref="Newest"/>.</summary>
    Relevance,
    Newest,
    Oldest,
    Updated,
    Title
}

/// <summary>
/// Optional filters of the admin article list; every set filter must match. <see cref="Sort"/> null means
/// relevance when <see cref="Search"/> is given, otherwise newest first.
/// </summary>
public record ArticleAdminFilter
{
    public const int MaxSearchLength = 200;

    public string? Search { get; init; }
    public ArticleAdminStatusFilter? Status { get; init; }
    public int? CategoryId { get; init; }
    public int? AuthorId { get; init; }
    public ArticleAdminSort? Sort { get; init; }
}
