using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

/// <summary>
/// GET /api/v1/search/suggest and GET /api/admin/articles/suggest, which are answered from the in-memory suggestion
/// index. Rows are written with plain SQL (ids from 12000), so every change has to travel through the database
/// triggers, NOTIFY cms_suggest and the API's listener before it shows. Tests therefore poll until the expected answer
/// arrives. Every test uses made-up words of its own.
/// </summary>
[Collection("Database")]
public class SearchSuggestionsTests : IAsyncLifetime
{
    private const int FirstId = 12000;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _adminClient;
    private int _nextId = FirstId;

    public SearchSuggestionsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        _adminClient = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient());
    }

    public Task InitializeAsync() => CleanUpAsync();

    public Task DisposeAsync() => CleanUpAsync();

    [Theory]
    [InlineData("")]
    [InlineData("q=")]
    [InlineData("q=a")]
    [InlineData("q=%27%29%3A*%26%7C%21")] // '):*&|!
    [InlineData("q=%00%01%02")]
    public async Task Suggest_WithBlankShortOrSymbolInput_ReturnsEmptyList(string queryString)
    {
        var response = await _client.GetAsync($"/api/v1/search/suggest?{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchSuggestionDto>>>();
        result!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Suggest_NewArticle_IsSuggestedByWordPrefixWithItsUrl()
    {
        await InsertArticleAsync("Getting started with quillfrond");

        var suggestions = await EventuallyPublicAsync("quillf", s => s.Count > 0);

        suggestions.Should().ContainSingle().Which.Should().Be(
            new SearchSuggestionDto("article", "Getting started with quillfrond", $"/blog/sugg-{FirstId}"));
        (await PublicAsync("start quill")).Should().ContainSingle("words match in any order and anywhere");
        (await PublicAsync("illfrond")).Should().BeEmpty("only the start of a word matches");
    }

    [Fact]
    public async Task Suggest_FollowsRenameUnpublishAndDelete()
    {
        var id = await InsertArticleAsync("Marrowpike notes");
        await EventuallyPublicAsync("marrowp", s => s.Count == 1);

        await ExecuteAsync($"UPDATE articles SET title = 'Thistlequay notes' WHERE id = {id}");
        await EventuallyPublicAsync("thistleq", s => s.Count == 1);
        (await PublicAsync("marrowp")).Should().BeEmpty();

        await ExecuteAsync($"UPDATE articles SET status = {(int)ArticleStatus.Draft} WHERE id = {id}");
        await EventuallyPublicAsync("thistleq", s => s.Count == 0);
        (await AdminAsync("thistleq")).Should().ContainSingle().Which.Status.Should().Be(ArticleStatus.Draft);

        await ExecuteAsync($"DELETE FROM articles WHERE id = {id}");
        await EventuallyAdminAsync("thistleq", s => s.Count == 0);
    }

    [Fact]
    public async Task Suggest_HidesScheduledArticlesAndPages()
    {
        await InsertArticleAsync("Scheduled glimmerhock", publishedAt: DateTime.UtcNow.AddDays(3));
        await InsertArticleAsync("Page glimmerhock", contentType: ContentType.Page);
        await InsertArticleAsync("Visible glimmerhock");

        await EventuallyAdminAsync("glimmerh", s => s.Count == 2);
        (await PublicAsync("glimmerh")).Select(s => s.Title).Should().Equal("Visible glimmerhock");
    }

    [Fact]
    public async Task Suggest_MembersOnlyArticles_AreSuggestedToSignedInReadersOnly()
    {
        await InsertArticleAsync("Members fennelgrove", requiresSignIn: true);
        await EventuallyAdminAsync("fennelg", s => s.Count == 1);

        (await PublicAsync("fennelg")).Should().BeEmpty();
        var reader = AuthHelper.CreateWebsiteUserAuthenticatedClient(_fixture.Factory.CreateClient());
        (await PublicAsync("fennelg", reader)).Should().ContainSingle();
        (await PublicAsync("fennelg", _adminClient)).Should().BeEmpty("only a website-reader token unlocks members-only");
    }

    [Fact]
    public async Task Suggest_TagsCategoriesAndSeries_FollowTheirArticles()
    {
        var tagId = _nextId++;
        var categoryId = _nextId++;
        var seriesId = _nextId++;
        await ExecuteAsync($@"
            INSERT INTO tags (id, name, slug, created_at) VALUES ({tagId}, 'Brambleworth tag', 'brambleworth-tag', now());
            INSERT INTO categories (id, name, slug, created_at) VALUES ({categoryId}, 'Brambleworth category', 'brambleworth-cat', now());
            INSERT INTO series (id, name, slug, created_at) VALUES ({seriesId}, 'Brambleworth series', 'brambleworth-series', now());");

        // No article yet: nothing to suggest.
        var articleId = await InsertArticleAsync("Unrelated title", status: ArticleStatus.Draft);
        await EventuallyAdminAsync("unrelated", s => s.Any(a => a.Id == articleId));
        (await PublicAsync("brambl")).Should().BeEmpty();

        await ExecuteAsync($@"
            UPDATE articles SET status = {(int)ArticleStatus.Published}, category_id = {categoryId}, series_id = {seriesId}
            WHERE id = {articleId};
            INSERT INTO article_tags (article_id, tag_id) VALUES ({articleId}, {tagId});");

        var suggestions = await EventuallyPublicAsync("brambl", s => s.Count == 3);
        suggestions.Should().Equal(
            new SearchSuggestionDto("tag", "Brambleworth tag", "/tag/brambleworth-tag"),
            new SearchSuggestionDto("category", "Brambleworth category", "/category/brambleworth-cat"),
            new SearchSuggestionDto("series", "Brambleworth series", "/series/brambleworth-series"));

        await ExecuteAsync($"UPDATE tags SET name = 'Hazelworth tag' WHERE id = {tagId}");
        await EventuallyPublicAsync("hazelw", s => s.Count == 1);

        await ExecuteAsync($"DELETE FROM article_tags WHERE tag_id = {tagId}");
        await EventuallyPublicAsync("hazelw", s => s.Count == 0);
    }

    [Fact]
    public async Task AdminSuggest_RequiresAnAdminToken()
    {
        (await _client.GetAsync("/api/admin/articles/suggest?q=anything")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var reader = AuthHelper.CreateWebsiteUserAuthenticatedClient(_fixture.Factory.CreateClient());
        (await reader.GetAsync("/api/admin/articles/suggest?q=anything")).StatusCode
            .Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminSuggest_ReturnsEveryStatusWithStatusAndPublishDate()
    {
        var draft = await InsertArticleAsync("Draft corvanish", status: ArticleStatus.Draft);
        var archived = await InsertArticleAsync("Archived corvanish", status: ArticleStatus.Archived);
        var scheduledAt = DateTime.UtcNow.AddDays(2);
        var scheduled = await InsertArticleAsync("Scheduled corvanish", publishedAt: scheduledAt);

        var suggestions = await EventuallyAdminAsync("corvan", s => s.Count == 3);

        suggestions.Select(s => s.Id).Should().Equal(scheduled, archived, draft);
        suggestions[0].PublishedAt.Should().BeCloseTo(scheduledAt, TimeSpan.FromSeconds(1));
        (await AdminAsync("corvan", limit: 1)).Should().HaveCount(1);
    }

    private async Task<List<SearchSuggestionDto>> PublicAsync(string query, HttpClient? client = null)
    {
        var response = await (client ?? _client).GetAsync($"/api/v1/search/suggest?q={Uri.EscapeDataString(query)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchSuggestionDto>>>())!.Data;
    }

    private async Task<List<ArticleSuggestionDto>> AdminAsync(string query, int limit = 8)
    {
        var response = await _adminClient.GetAsync($"/api/admin/articles/suggest?q={Uri.EscapeDataString(query)}&limit={limit}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<List<ArticleSuggestionDto>>>())!.Data;
    }

    private Task<List<SearchSuggestionDto>> EventuallyPublicAsync(string query, Func<List<SearchSuggestionDto>, bool> done) =>
        EventuallyAsync(() => PublicAsync(query), done, query);

    private Task<List<ArticleSuggestionDto>> EventuallyAdminAsync(string query, Func<List<ArticleSuggestionDto>, bool> done) =>
        EventuallyAsync(() => AdminAsync(query), done, query);

    /// <summary>Polls until the index has caught up with the database (or fails after <see cref="Timeout"/>).</summary>
    private static async Task<T> EventuallyAsync<T>(Func<Task<T>> fetch, Func<T, bool> done, string query)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            var result = await fetch();
            if (done(result))
            {
                return result;
            }
            if (DateTime.UtcNow > deadline)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Suggestions for '{query}' did not reach the expected state within {Timeout.TotalSeconds} s");
            }
            await Task.Delay(50);
        }
    }

    private async Task<int> InsertArticleAsync(
        string title,
        ArticleStatus status = ArticleStatus.Published,
        ContentType contentType = ContentType.Article,
        bool requiresSignIn = false,
        DateTime? publishedAt = null)
    {
        var id = _nextId++;
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(@"
            INSERT INTO articles (id, title, slug, content_type, author_id, status, requires_sign_in, published_at, created_at)
            VALUES (@id, @title, @slug, @contentType, 1, @status, @requiresSignIn, @publishedAt, now())");
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@slug", $"sugg-{id}");
        command.Parameters.AddWithValue("@contentType", (int)contentType);
        command.Parameters.AddWithValue("@status", (int)status);
        command.Parameters.AddWithValue("@requiresSignIn", requiresSignIn);
        command.Parameters.AddWithValue("@publishedAt", (object?)publishedAt ?? DateTime.UtcNow.AddMinutes(-1));
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private static Task CleanUpAsync() => ExecuteAsync($@"
        DELETE FROM articles WHERE id >= {FirstId} AND id < {FirstId + 1000};
        DELETE FROM tags WHERE id >= {FirstId} AND id < {FirstId + 1000};
        DELETE FROM categories WHERE id >= {FirstId} AND id < {FirstId + 1000};
        DELETE FROM series WHERE id >= {FirstId} AND id < {FirstId + 1000};");
}
