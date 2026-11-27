using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Suggestions;
using SharpQuill.Core.Articles.Suggestions;

namespace SharpQuill.Application.Articles.Services;

public class SearchSuggestionService : ISearchSuggestionService
{
    public const int MaxAdminLimit = 20;

    private static readonly PublicSuggestionLimits PublicLimits = new();

    private readonly SuggestionIndexHolder _holder;
    private readonly TimeProvider _timeProvider;

    public SearchSuggestionService(SuggestionIndexHolder holder, TimeProvider timeProvider)
    {
        _holder = holder;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<SearchSuggestionDto> SuggestPublic(string? query, bool includeMembersOnly)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return _holder.Current.SuggestPublic(query, includeMembersOnly, now, PublicLimits)
            .Select(s => new SearchSuggestionDto(TypeName(s.Kind), s.Title, Url(s)))
            .ToList();
    }

    public IReadOnlyList<ArticleSuggestionDto> SuggestAdmin(string? query, int limit)
    {
        return _holder.Current.SuggestAdmin(query, Math.Clamp(limit, 1, MaxAdminLimit))
            .Select(a => new ArticleSuggestionDto(a.Id, a.Title, a.Status, a.PublishedAt))
            .ToList();
    }

    private static string TypeName(SuggestionKind kind) => kind switch
    {
        SuggestionKind.Article => "article",
        SuggestionKind.Tag => "tag",
        SuggestionKind.Category => "category",
        SuggestionKind.Series => "series",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>Website routes: /blog/{slug}, /tag/{slug}, /category/{slug}, /series/{slug}.</summary>
    private static string Url(PublicSuggestion s)
    {
        var slug = Uri.EscapeDataString(s.Slug);
        return s.Kind switch
        {
            SuggestionKind.Article => $"/blog/{slug}",
            SuggestionKind.Tag => $"/tag/{slug}",
            SuggestionKind.Category => $"/category/{slug}",
            SuggestionKind.Series => $"/series/{slug}",
            _ => throw new ArgumentOutOfRangeException(nameof(s), s.Kind, null)
        };
    }
}
