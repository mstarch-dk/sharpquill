using SharpQuill.Core.Articles;
using SharpQuill.Core.Articles.Suggestions;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>The in-memory search-suggestion index, without a database.</summary>
public class SuggestionIndexTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly PublicSuggestionLimits Limits = new();

    private static SuggestionArticle Article(
        int id, string title,
        ArticleStatus status = ArticleStatus.Published,
        DateTime? publishedAt = null,
        bool requiresSignIn = false,
        ContentType contentType = ContentType.Article,
        int? categoryId = null, int? seriesId = null, int[]? tagIds = null) =>
        new(id, title, $"slug-{id}", contentType, status, publishedAt ?? Now.AddDays(-id), requiresSignIn,
            categoryId, seriesId, tagIds ?? []);

    private static SuggestionIndex Index(
        IEnumerable<SuggestionArticle> articles,
        IEnumerable<SuggestionTerm>? tags = null,
        IEnumerable<SuggestionTerm>? categories = null,
        IEnumerable<SuggestionTerm>? series = null) =>
        SuggestionIndex.Build(new SuggestionData(articles.ToList(), (tags ?? []).ToList(),
            (categories ?? []).ToList(), (series ?? []).ToList()));

    private static IEnumerable<int> ArticleIds(IEnumerable<PublicSuggestion> suggestions) =>
        suggestions.Where(s => s.Kind == SuggestionKind.Article).Select(s => s.Id);

    [Theory]
    [InlineData("Hello, World!", new[] { "hello", "world" })]
    [InlineData("Ærø og Øresund", new[] { "ærø", "og", "øresund" })]
    [InlineData("C# 10 & .NET-tips", new[] { "c", "10", "net", "tips" })]
    [InlineData("   ", new string[0])]
    public void Tokenize_LowerCasesAndSplitsOnNonWordCharacters(string text, string[] expected)
    {
        SuggestionText.Tokenize(text).Should().Equal(expected);
    }

    [Fact]
    public void Tokenize_NormalizesDecomposedCharacters()
    {
        // "å" written as a + combining ring above
        SuggestionText.Tokenize("Gård").Should().Equal("gård");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("!!")]
    [InlineData("&|:*'()")]
    public void TokenizeQuery_IgnoresTooShortOrSymbolOnlyInput(string? query)
    {
        SuggestionText.TokenizeQuery(query).Should().BeEmpty();
    }

    [Fact]
    public void TokenizeQuery_DropsDuplicatesAndCapsTheWordCount()
    {
        SuggestionText.TokenizeQuery("a b a c d e f g").Should().Equal("a", "b", "c", "d", "e");
    }

    [Fact]
    public void SuggestPublic_MatchesTheStartOfAnyWord()
    {
        var index = Index([Article(1, "Getting started with PostgreSQL"), Article(2, "Unrelated"), Article(3, "Postcards")]);

        ArticleIds(index.SuggestPublic("post", false, Now, Limits)).Should().BeEquivalentTo([1, 3]);
        ArticleIds(index.SuggestPublic("tgre", false, Now, Limits)).Should().BeEmpty("only word starts match");
    }

    [Fact]
    public void SuggestPublic_RequiresEveryWordInAnyOrder()
    {
        var index = Index([Article(1, "PostgreSQL performance tuning"), Article(2, "PostgreSQL backups")]);

        ArticleIds(index.SuggestPublic("tun postg", false, Now, Limits)).Should().Equal(1);
        ArticleIds(index.SuggestPublic("POSTGRES", false, Now, Limits)).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void SuggestPublic_RanksTitlesThatStartWithTheQueryFirst()
    {
        var index = Index(
        [
            Article(1, "Why search matters", publishedAt: Now.AddDays(-1)),
            Article(2, "Search tips", publishedAt: Now.AddDays(-10)),
            Article(3, "A guide to search engines", publishedAt: Now.AddDays(-2))
        ]);

        // 2 starts with the query; 1 matches at word 1, 3 at word 3.
        ArticleIds(index.SuggestPublic("sea", false, Now, Limits)).Should().Equal(2, 1, 3);
    }

    [Fact]
    public void SuggestPublic_BreaksTiesByNewestPublishDate()
    {
        var index = Index(
        [
            Article(1, "Release notes", publishedAt: Now.AddDays(-30)),
            Article(2, "Release notes", publishedAt: Now.AddDays(-1))
        ]);

        ArticleIds(index.SuggestPublic("rel", false, Now, Limits)).Should().Equal(2, 1);
    }

    [Fact]
    public void SuggestPublic_HidesDraftsArchivedPagesAndScheduledArticles()
    {
        var index = Index(
        [
            Article(1, "Visible kelpie"),
            Article(2, "Draft kelpie", status: ArticleStatus.Draft),
            Article(3, "Archived kelpie", status: ArticleStatus.Archived),
            Article(4, "Page kelpie", contentType: ContentType.Page),
            Article(5, "Scheduled kelpie", publishedAt: Now.AddHours(1)),
            new SuggestionArticle(6, "Undated kelpie", "undated", ContentType.Article, ArticleStatus.Published,
                null, false, null, null, [])
        ]);

        ArticleIds(index.SuggestPublic("kelpie", false, Now, Limits)).Should().BeEquivalentTo([1, 6]);
        ArticleIds(index.SuggestPublic("kelpie", false, Now.AddHours(2), Limits)).Should().BeEquivalentTo([1, 5, 6],
            "a scheduled article turns up once its publish date has passed");
    }

    [Fact]
    public void SuggestPublic_SuggestsMembersOnlyArticlesToReadersOnly()
    {
        var index = Index([Article(1, "Members guide", requiresSignIn: true)]);

        index.SuggestPublic("memb", false, Now, Limits).Should().BeEmpty();
        ArticleIds(index.SuggestPublic("memb", true, Now, Limits)).Should().Equal(1);
    }

    [Fact]
    public void SuggestPublic_SuggestsTermsThatHaveAPublicArticle()
    {
        var index = Index(
            [
                Article(1, "First", categoryId: 10, seriesId: 20, tagIds: [30]),
                Article(2, "Second", status: ArticleStatus.Draft, categoryId: 11, tagIds: [31]),
                Article(3, "Members", requiresSignIn: true, tagIds: [32])
            ],
            tags: [new(30, "Travel", "travel"), new(31, "Travel drafts", "travel-drafts"), new(32, "Travel members", "travel-members"), new(33, "Travel empty", "travel-empty")],
            categories: [new(10, "Travelling", "travelling"), new(11, "Travel plans", "travel-plans")],
            series: [new(20, "Travel diary", "travel-diary")]);

        var suggestions = index.SuggestPublic("trav", false, Now, new PublicSuggestionLimits(Tags: 5));

        suggestions.Should().BeEquivalentTo(new[]
        {
            new PublicSuggestion(SuggestionKind.Tag, 30, "Travel", "travel"),
            new PublicSuggestion(SuggestionKind.Tag, 32, "Travel members", "travel-members"),
            new PublicSuggestion(SuggestionKind.Category, 10, "Travelling", "travelling"),
            new PublicSuggestion(SuggestionKind.Series, 20, "Travel diary", "travel-diary")
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void SuggestPublic_AppliesLimitsPerKind()
    {
        var articles = Enumerable.Range(1, 10).Select(i => Article(i, $"Topic {i}", tagIds: [100 + i])).ToList();
        var tags = Enumerable.Range(1, 10).Select(i => new SuggestionTerm(100 + i, $"Topic tag {i}", $"t{i}")).ToList();
        var index = Index(articles, tags);

        var suggestions = index.SuggestPublic("topic", false, Now, new PublicSuggestionLimits(Articles: 3, Tags: 1));

        suggestions.Count(s => s.Kind == SuggestionKind.Article).Should().Be(3);
        suggestions.Count(s => s.Kind == SuggestionKind.Tag).Should().Be(1);
    }

    [Fact]
    public void SuggestAdmin_IncludesEveryStatusButNoPages()
    {
        var index = Index(
        [
            Article(1, "Draft wombat", status: ArticleStatus.Draft),
            Article(2, "Archived wombat", status: ArticleStatus.Archived),
            Article(3, "Scheduled wombat", publishedAt: Now.AddDays(5)),
            Article(4, "Page wombat", contentType: ContentType.Page)
        ]);

        index.SuggestAdmin("womb", 10).Select(a => a.Id).Should().Equal(3, 2, 1);
        index.SuggestAdmin("womb", 2).Should().HaveCount(2);
    }

    [Fact]
    public void Apply_RenamesUnpublishesAndDeletesWithoutChangingTheOldIndex()
    {
        var before = Index([Article(1, "Old name"), Article(2, "Other article"), Article(3, "Doomed article")]);

        var after = before.Apply(new SuggestionDelta
        {
            ChangedArticles = [Article(1, "New name"), Article(2, "Other article", status: ArticleStatus.Draft)],
            DeletedArticleIds = [3]
        });

        ArticleIds(after.SuggestPublic("new", false, Now, Limits)).Should().Equal(1);
        after.SuggestPublic("old", false, Now, Limits).Should().BeEmpty();
        after.SuggestPublic("art", false, Now, Limits).Should().BeEmpty();
        after.ArticleCount.Should().Be(2);

        ArticleIds(before.SuggestPublic("old", false, Now, Limits)).Should().Equal(1);
        ArticleIds(before.SuggestPublic("art", false, Now, Limits)).Should().BeEquivalentTo([2, 3]);
    }

    [Fact]
    public void Apply_UpdatesTermsAndTagLinks()
    {
        var before = Index([Article(1, "Story", tagIds: [10])], tags: [new(10, "Cooking", "cooking"), new(11, "Baking", "baking")]);

        var after = before.Apply(new SuggestionDelta
        {
            ChangedArticles = [Article(1, "Story", tagIds: [11])],
            ChangedTerms = new Dictionary<SuggestionKind, IReadOnlyCollection<SuggestionTerm>>
            {
                [SuggestionKind.Tag] = [new(11, "Baking bread", "baking-bread")]
            },
            DeletedTermIds = new Dictionary<SuggestionKind, IReadOnlyCollection<int>>
            {
                [SuggestionKind.Category] = [999]
            }
        });

        after.SuggestPublic("cook", false, Now, Limits).Should().BeEmpty("the article no longer has that tag");
        after.SuggestPublic("bread", false, Now, Limits).Should().ContainSingle()
            .Which.Should().Be(new PublicSuggestion(SuggestionKind.Tag, 11, "Baking bread", "baking-bread"));
    }

    [Fact]
    public void Empty_AnswersNothing()
    {
        SuggestionIndex.Empty.SuggestPublic("anything", true, Now, Limits).Should().BeEmpty();
        SuggestionIndex.Empty.SuggestAdmin("anything", 5).Should().BeEmpty();
    }
}
