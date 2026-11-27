namespace SharpQuill.Core.Articles.Suggestions;

/// <summary>
/// An immutable, in-memory word-prefix index over article titles and tag, category and series names, used to answer
/// search-as-you-type suggestions without a database query.
/// <para>
/// Every word of every title and name goes into one array sorted by ordinal order. A query word is matched by a
/// binary search for its prefix range ("right truncation"). A query with several words takes its candidates from the
/// narrowest range and keeps those in which every other query word is also the start of some word, in any order.
/// </para>
/// <para>
/// Visibility is decided when a query is answered, not when the index is built: a scheduled article turns up once its
/// publish date has passed, with no change signal needed. Changes produce a new index (<see cref="Apply"/>), so
/// readers can share an instance across threads without locks.
/// </para>
/// </summary>
public sealed class SuggestionIndex
{
    public static readonly SuggestionIndex Empty = Build(SuggestionData.Empty);

    private readonly Dictionary<int, SuggestionArticle> _articles;
    private readonly Dictionary<SuggestionKind, Dictionary<int, SuggestionTerm>> _terms;
    private readonly Dictionary<(SuggestionKind Kind, int Id), List<int>> _termArticleIds;
    private readonly Entry[] _entries;
    private readonly Posting[] _postings;

    private sealed record Entry(SuggestionKind Kind, int Id, string Title, string[] Tokens);

    private readonly record struct Posting(string Token, int EntryIndex);

    private readonly record struct Match(Entry Entry, bool Leading, int PositionSum);

    private SuggestionIndex(
        Dictionary<int, SuggestionArticle> articles,
        Dictionary<SuggestionKind, Dictionary<int, SuggestionTerm>> terms)
    {
        _articles = articles;
        _terms = terms;

        _termArticleIds = new Dictionary<(SuggestionKind, int), List<int>>();
        foreach (var article in articles.Values)
        {
            if (article.CategoryId is { } categoryId)
            {
                AddTermArticle(SuggestionKind.Category, categoryId, article.Id);
            }
            if (article.SeriesId is { } seriesId)
            {
                AddTermArticle(SuggestionKind.Series, seriesId, article.Id);
            }
            foreach (var tagId in article.TagIds)
            {
                AddTermArticle(SuggestionKind.Tag, tagId, article.Id);
            }
        }

        var entries = new List<Entry>(articles.Count + terms.Values.Sum(t => t.Count));
        // Pages are never suggested (public search and the admin article list cover articles only).
        entries.AddRange(articles.Values
            .Where(a => a.ContentType == ContentType.Article)
            .Select(a => new Entry(SuggestionKind.Article, a.Id, a.Title, SuggestionText.Tokenize(a.Title))));
        foreach (var (kind, byId) in terms)
        {
            entries.AddRange(byId.Values.Select(t => new Entry(kind, t.Id, t.Name, SuggestionText.Tokenize(t.Name))));
        }
        _entries = entries.ToArray();

        var postings = new List<Posting>(_entries.Sum(e => e.Tokens.Length));
        for (var i = 0; i < _entries.Length; i++)
        {
            foreach (var token in _entries[i].Tokens.Distinct(StringComparer.Ordinal))
            {
                postings.Add(new Posting(token, i));
            }
        }
        postings.Sort((a, b) => string.CompareOrdinal(a.Token, b.Token));
        _postings = postings.ToArray();
    }

    private void AddTermArticle(SuggestionKind kind, int termId, int articleId)
    {
        if (!_termArticleIds.TryGetValue((kind, termId), out var ids))
        {
            ids = [];
            _termArticleIds[(kind, termId)] = ids;
        }
        ids.Add(articleId);
    }

    public int ArticleCount => _articles.Count;

    public int TermCount => _terms.Values.Sum(t => t.Count);

    public static SuggestionIndex Build(SuggestionData data)
    {
        static Dictionary<int, SuggestionTerm> ById(IEnumerable<SuggestionTerm> terms) => terms.ToDictionary(t => t.Id);

        return new SuggestionIndex(
            data.Articles.ToDictionary(a => a.Id),
            new Dictionary<SuggestionKind, Dictionary<int, SuggestionTerm>>
            {
                [SuggestionKind.Tag] = ById(data.Tags),
                [SuggestionKind.Category] = ById(data.Categories),
                [SuggestionKind.Series] = ById(data.Series)
            });
    }

    /// <summary>A new index with the changes applied; this one is left unchanged.</summary>
    public SuggestionIndex Apply(SuggestionDelta delta)
    {
        var articles = new Dictionary<int, SuggestionArticle>(_articles);
        foreach (var id in delta.DeletedArticleIds)
        {
            articles.Remove(id);
        }
        foreach (var article in delta.ChangedArticles)
        {
            articles[article.Id] = article;
        }

        var terms = _terms.ToDictionary(kv => kv.Key, kv => new Dictionary<int, SuggestionTerm>(kv.Value));
        foreach (var (kind, ids) in delta.DeletedTermIds)
        {
            foreach (var id in ids)
            {
                terms[kind].Remove(id);
            }
        }
        foreach (var (kind, changed) in delta.ChangedTerms)
        {
            foreach (var term in changed)
            {
                terms[kind][term.Id] = term;
            }
        }

        return new SuggestionIndex(articles, terms);
    }

    /// <summary>
    /// Suggestions for the public site: published articles whose publish date has passed (members-only ones only when
    /// <paramref name="includeMembersOnly"/>), and tags, categories and series that have at least one public article.
    /// </summary>
    public IReadOnlyList<PublicSuggestion> SuggestPublic(
        string? query, bool includeMembersOnly, DateTime nowUtc, PublicSuggestionLimits limits)
    {
        var matches = FindMatches(query);
        if (matches.Count == 0)
        {
            return [];
        }

        // Same rule as the public_articles view.
        bool IsPublic(SuggestionArticle a) =>
            a.ContentType == ContentType.Article
            && a.Status == ArticleStatus.Published
            && (a.PublishedAt is null || a.PublishedAt.Value <= nowUtc);

        // Like site search, members-only articles are suggested to signed-in readers only.
        bool IsVisible(SuggestionArticle a) => IsPublic(a) && (includeMembersOnly || !a.RequiresSignIn);

        // Tag, category and series pages list members-only articles too (as teasers), so those count for anyone.
        bool HasVisibleArticle(SuggestionKind kind, int id) =>
            _termArticleIds.TryGetValue((kind, id), out var ids) && ids.Any(articleId => IsPublic(_articles[articleId]));

        IEnumerable<PublicSuggestion> Take(SuggestionKind kind, int limit)
        {
            if (limit <= 0)
            {
                return [];
            }

            var ofKind = matches.Where(m => m.Entry.Kind == kind);
            if (kind == SuggestionKind.Article)
            {
                return ofKind
                    .Select(m => (Match: m, Article: _articles[m.Entry.Id]))
                    .Where(x => IsVisible(x.Article))
                    .OrderBy(x => x.Match.Leading ? 0 : 1)
                    .ThenBy(x => x.Match.PositionSum)
                    .ThenByDescending(x => x.Article.PublishedAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Article.Id)
                    .Take(limit)
                    .Select(x => new PublicSuggestion(kind, x.Article.Id, x.Article.Title, x.Article.Slug));
            }

            return ofKind
                .Where(m => HasVisibleArticle(kind, m.Entry.Id))
                .OrderBy(m => m.Leading ? 0 : 1)
                .ThenBy(m => m.PositionSum)
                .ThenBy(m => m.Entry.Title, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(m => m.Entry.Id)
                .Take(limit)
                .Select(m => new PublicSuggestion(kind, m.Entry.Id, m.Entry.Title, _terms[kind][m.Entry.Id].Slug));
        }

        return
        [
            .. Take(SuggestionKind.Article, limits.Articles),
            .. Take(SuggestionKind.Tag, limits.Tags),
            .. Take(SuggestionKind.Category, limits.Categories),
            .. Take(SuggestionKind.Series, limits.Series)
        ];
    }

    /// <summary>Suggestions for the admin: articles in every status, newest (highest id) first among equal matches.</summary>
    public IReadOnlyList<SuggestionArticle> SuggestAdmin(string? query, int limit)
    {
        return FindMatches(query)
            .Where(m => m.Entry.Kind == SuggestionKind.Article)
            .OrderBy(m => m.Leading ? 0 : 1)
            .ThenBy(m => m.PositionSum)
            .ThenByDescending(m => m.Entry.Id)
            .Take(Math.Max(limit, 0))
            .Select(m => _articles[m.Entry.Id])
            .ToList();
    }

    /// <summary>
    /// Entries in which every query word is the start of a word. <see cref="Match.Leading"/>: the title starts with
    /// the query as typed (all words but the last complete and in order); <see cref="Match.PositionSum"/>: how early
    /// in the title the query words matched.
    /// </summary>
    private List<Match> FindMatches(string? query)
    {
        var queryTokens = SuggestionText.TokenizeQuery(query);
        if (queryTokens.Length == 0)
        {
            return [];
        }

        var ranges = new (int Start, int End)[queryTokens.Length];
        for (var i = 0; i < queryTokens.Length; i++)
        {
            ranges[i] = PrefixRange(queryTokens[i]);
            if (ranges[i].Start == ranges[i].End)
            {
                return [];
            }
        }

        var narrowest = ranges.MinBy(r => r.End - r.Start);
        var candidates = new HashSet<int>();
        for (var p = narrowest.Start; p < narrowest.End; p++)
        {
            candidates.Add(_postings[p].EntryIndex);
        }

        var matches = new List<Match>(candidates.Count);
        foreach (var entryIndex in candidates)
        {
            var entry = _entries[entryIndex];
            var positionSum = 0;
            var all = true;
            foreach (var queryToken in queryTokens)
            {
                var position = Array.FindIndex(entry.Tokens, t => t.StartsWith(queryToken, StringComparison.Ordinal));
                if (position < 0)
                {
                    all = false;
                    break;
                }
                positionSum += position;
            }

            if (all)
            {
                matches.Add(new Match(entry, StartsWithQuery(entry.Tokens, queryTokens), positionSum));
            }
        }

        return matches;
    }

    private static bool StartsWithQuery(string[] tokens, string[] queryTokens)
    {
        if (tokens.Length < queryTokens.Length)
        {
            return false;
        }

        var last = queryTokens.Length - 1;
        for (var i = 0; i < last; i++)
        {
            if (!string.Equals(tokens[i], queryTokens[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return tokens[last].StartsWith(queryTokens[last], StringComparison.Ordinal);
    }

    /// <summary>The postings whose word starts with <paramref name="prefix"/>: [Start, End).</summary>
    private (int Start, int End) PrefixRange(string prefix)
    {
        int low = 0, high = _postings.Length;
        while (low < high)
        {
            var mid = (low + high) >>> 1;
            if (string.CompareOrdinal(_postings[mid].Token, prefix) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        var end = low;
        while (end < _postings.Length && _postings[end].Token.StartsWith(prefix, StringComparison.Ordinal))
        {
            end++;
        }

        return (low, end);
    }
}
