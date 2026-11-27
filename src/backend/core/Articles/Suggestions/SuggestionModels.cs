namespace SharpQuill.Core.Articles.Suggestions;

/// <summary>What a search suggestion points at.</summary>
public enum SuggestionKind
{
    Article,
    Tag,
    Category,
    Series
}

/// <summary>The fields of an article (or page) that the suggestion index needs.</summary>
public record SuggestionArticle(
    int Id,
    string Title,
    string Slug,
    ContentType ContentType,
    ArticleStatus Status,
    DateTime? PublishedAt,
    bool RequiresSignIn,
    int? CategoryId,
    int? SeriesId,
    IReadOnlyList<int> TagIds);

/// <summary>A tag, category or series: a name to match and a slug to link to.</summary>
public record SuggestionTerm(int Id, string Name, string Slug);

/// <summary>Everything the index holds; what a full load from the database returns.</summary>
public record SuggestionData(
    IReadOnlyCollection<SuggestionArticle> Articles,
    IReadOnlyCollection<SuggestionTerm> Tags,
    IReadOnlyCollection<SuggestionTerm> Categories,
    IReadOnlyCollection<SuggestionTerm> Series)
{
    public static readonly SuggestionData Empty = new([], [], [], []);
}

/// <summary>
/// Changes since the previous snapshot. <c>Changed*</c> are the current rows of the ids that changed; an id listed
/// in <c>Deleted*</c> no longer exists.
/// </summary>
public record SuggestionDelta
{
    public IReadOnlyCollection<SuggestionArticle> ChangedArticles { get; init; } = [];
    public IReadOnlyCollection<int> DeletedArticleIds { get; init; } = [];
    public IReadOnlyDictionary<SuggestionKind, IReadOnlyCollection<SuggestionTerm>> ChangedTerms { get; init; } =
        new Dictionary<SuggestionKind, IReadOnlyCollection<SuggestionTerm>>();
    public IReadOnlyDictionary<SuggestionKind, IReadOnlyCollection<int>> DeletedTermIds { get; init; } =
        new Dictionary<SuggestionKind, IReadOnlyCollection<int>>();
}

/// <summary>One public suggestion: an article or a tag, category or series name.</summary>
public record PublicSuggestion(SuggestionKind Kind, int Id, string Title, string Slug);

/// <summary>How many suggestions of each kind a public query returns.</summary>
public record PublicSuggestionLimits(int Articles = 6, int Tags = 2, int Categories = 2, int Series = 2);
