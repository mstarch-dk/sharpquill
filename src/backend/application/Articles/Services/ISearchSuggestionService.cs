using SharpQuill.Application.Articles.DTOs;

namespace SharpQuill.Application.Articles.Services;

/// <summary>Search-as-you-type suggestions, answered from the in-memory suggestion index.</summary>
public interface ISearchSuggestionService
{
    /// <summary>Public site: matching article titles, then tag, category and series names.</summary>
    IReadOnlyList<SearchSuggestionDto> SuggestPublic(string? query, bool includeMembersOnly);

    /// <summary>Admin: matching article titles in every status.</summary>
    IReadOnlyList<ArticleSuggestionDto> SuggestAdmin(string? query, int limit);
}
