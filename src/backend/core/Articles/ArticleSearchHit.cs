namespace SharpQuill.Core.Articles;

/// <summary>
/// One full-text search match: the article and a text snippet in which every matched word is wrapped in
/// <see cref="ArticleSearch.MatchStart"/> and <see cref="ArticleSearch.MatchEnd"/>.
/// </summary>
public record ArticleSearchHit(Article Article, string Snippet);

public static class ArticleSearch
{
    /// <summary>Marks the start of a matched word in <see cref="ArticleSearchHit.Snippet"/>; never part of article text.</summary>
    public const char MatchStart = '\u0002';

    /// <summary>Marks the end of a matched word in <see cref="ArticleSearchHit.Snippet"/>.</summary>
    public const char MatchEnd = '\u0003';

    /// <summary>Longer queries are cut to this many characters.</summary>
    public const int MaxQueryLength = 200;
}
