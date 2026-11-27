namespace SharpQuill.Core.Articles.Suggestions;

/// <summary>Reads what the search-suggestion index holds: everything at once, or the current rows of changed ids.</summary>
public interface ISuggestionIndexRepository
{
    Task<SuggestionData> LoadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The current rows of these articles; an id without a row has been deleted.</summary>
    Task<IReadOnlyList<SuggestionArticle>> LoadArticlesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    /// <summary>The current rows of these tags, categories or series; an id without a row has been deleted.</summary>
    Task<IReadOnlyList<SuggestionTerm>> LoadTermsAsync(
        SuggestionKind kind, IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
