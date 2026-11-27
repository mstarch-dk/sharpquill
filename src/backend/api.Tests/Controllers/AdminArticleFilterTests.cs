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

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// Search, filters and sort of GET /api/admin/articles. Each test searches for made-up words that only its own articles
/// contain, so seed data and other test classes do not affect the results. Articles are written with SQL (ids from
/// 10000), which also exercises the triggers that maintain articles.admin_search_vector.
/// </summary>
[Collection("Database")]
public class AdminArticleFilterTests : IAsyncLifetime
{
    private const int FirstTestArticleId = 10000;
    private const int OtherAuthorId = 2; // CertUser in the seed data
    private const int TechnologyCategoryId = 2;

    private readonly HttpClient _client;
    private int _nextArticleId = FirstTestArticleId;

    public AdminArticleFilterTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    public Task InitializeAsync() => DeleteTestArticlesAsync();

    public Task DisposeAsync() => DeleteTestArticlesAsync();

    [Fact]
    public async Task Search_FindsDraftAndArchivedArticlesByBodyText()
    {
        var draft = await InsertArticleAsync("Draft one", "Unpublished body with plimwasser inside.", ArticleStatus.Draft);
        var archived = await InsertArticleAsync("Old one", "Archived body with plimwasser too.", ArticleStatus.Archived);

        var ids = await GetIdsAsync("search=plimwasser&sort=oldest");

        ids.Should().Equal(draft, archived);
    }

    [Fact]
    public async Task Search_UsesLatestVersionNotPublishedVersion()
    {
        var id = await InsertArticleAsync("Versioned", "Published text mentions graufelden.");
        await AddVersionAsync(id, 2, "The unpublished edit mentions torvikkel instead.");

        (await GetIdsAsync("search=torvikkel")).Should().Equal(id);
        (await GetIdsAsync("search=graufelden")).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_MatchesPartOfTitle()
    {
        var id = await InsertArticleAsync("Notes on snarbliquet tuning", "Body");

        (await GetIdsAsync("search=snarbliq")).Should().Equal(id);
    }

    [Fact]
    public async Task Search_TreatsLikeWildcardsLiterally()
    {
        await InsertArticleAsync("Wildcard wexterpol", "Body");

        (await GetIdsAsync("search=wexter%25pol")).Should().BeEmpty();
        (await GetIdsAsync("search=wexte_pol")).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_RanksTitleMatchFirst()
    {
        var bodyMatch = await InsertArticleAsync("Plain title", "The body mentions quorvantis once.");
        var titleMatch = await InsertArticleAsync("All about quorvantis", "Nothing else.");

        (await GetIdsAsync("search=quorvantis")).Should().Equal(titleMatch, bodyMatch);
    }

    [Fact]
    public async Task Filters_CombineTextStatusCategoryAndAuthor()
    {
        var match = await InsertArticleAsync("Match zentrafol", "Body", ArticleStatus.Draft,
            categoryId: TechnologyCategoryId, authorId: OtherAuthorId);
        await InsertArticleAsync("Wrong status zentrafol", "Body", ArticleStatus.Published,
            categoryId: TechnologyCategoryId, authorId: OtherAuthorId);
        await InsertArticleAsync("Wrong category zentrafol", "Body", ArticleStatus.Draft,
            categoryId: 1, authorId: OtherAuthorId);
        await InsertArticleAsync("Wrong author zentrafol", "Body", ArticleStatus.Draft,
            categoryId: TechnologyCategoryId, authorId: TestSeedData.AdminAuthorId);
        await InsertArticleAsync("Other text", "Body", ArticleStatus.Draft,
            categoryId: TechnologyCategoryId, authorId: OtherAuthorId);

        var result = await GetAsync(
            $"search=zentrafol&status=draft&categoryId={TechnologyCategoryId}&authorId={OtherAuthorId}");

        result.Data.Select(a => a.Id).Should().Equal(match);
        Meta(result).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task StatusFilter_SplitsPublishedAndScheduled()
    {
        var live = await InsertArticleAsync("Live brontiquel", "Body", ArticleStatus.Published,
            publishedAt: DateTime.UtcNow.AddDays(-1));
        var scheduled = await InsertArticleAsync("Scheduled brontiquel", "Body", ArticleStatus.Published,
            publishedAt: DateTime.UtcNow.AddDays(3));

        (await GetIdsAsync("search=brontiquel&status=published")).Should().Equal(live);
        (await GetIdsAsync("search=brontiquel&status=Scheduled")).Should().Equal(scheduled);
    }

    [Fact]
    public async Task Sort_ByTitle_IsAlphabeticalAndCaseInsensitive()
    {
        var c = await InsertArticleAsync("charlie fendrisk", "Body");
        var a = await InsertArticleAsync("Alpha fendrisk", "Body");
        var b = await InsertArticleAsync("bravo fendrisk", "Body");

        (await GetIdsAsync("search=fendrisk&sort=title")).Should().Equal(a, b, c);
    }

    [Fact]
    public async Task Filters_KeepPaginationMeta()
    {
        for (var i = 0; i < 3; i++)
        {
            await InsertArticleAsync($"Paged moltrevin {i}", "Body");
        }

        var result = await GetAsync("search=moltrevin&page=2&pageSize=2");

        result.Data.Should().HaveCount(1);
        Meta(result).GetProperty("totalCount").GetInt32().Should().Be(3);
        Meta(result).GetProperty("totalPages").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Filters_ExcludePages()
    {
        await InsertArticleAsync("A page about drenskovar", "Body", contentType: ContentType.Page);

        (await GetIdsAsync("search=drenskovar")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("status=deleted")]
    [InlineData("status=1")]
    [InlineData("sort=random")]
    public async Task InvalidStatusOrSort_ReturnsBadRequest(string queryString)
    {
        var response = await _client.GetAsync($"/api/admin/articles?{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    private async Task<List<int>> GetIdsAsync(string queryString) =>
        (await GetAsync(queryString)).Data.Select(a => a.Id).ToList();

    private async Task<ApiResponse<List<ArticleAdminDto>>> GetAsync(string queryString)
    {
        var response = await _client.GetAsync($"/api/admin/articles?{queryString}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ArticleAdminDto>>>();
        result.Should().NotBeNull();
        return result!;
    }

    private static System.Text.Json.JsonElement Meta(ApiResponse<List<ArticleAdminDto>> result) =>
        result.Meta.Should().BeOfType<System.Text.Json.JsonElement>().Subject;

    private async Task<int> InsertArticleAsync(
        string title,
        string content,
        ArticleStatus status = ArticleStatus.Published,
        int? categoryId = null,
        int authorId = TestSeedData.AdminAuthorId,
        ContentType contentType = ContentType.Article,
        DateTime? publishedAt = null)
    {
        var id = _nextArticleId++;
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(@"
            INSERT INTO articles (id, title, slug, content_type, author_id, category_id, status, published_at, created_at)
            VALUES (@id, @title, @slug, @contentType, @authorId, @categoryId, @status, @publishedAt,
                    now() + make_interval(secs => @id - 10000));
            INSERT INTO article_contents (id, article_id, content, version_number, created_at)
            VALUES (@id * 10 + 1, @id, @content, 1, now());
            UPDATE articles SET published_version_id = @id * 10 + 1 WHERE id = @id AND status = 1;");
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("slug", $"admin-filter-test-{id}");
        command.Parameters.AddWithValue("contentType", (int)contentType);
        command.Parameters.AddWithValue("authorId", authorId);
        command.Parameters.AddWithValue("categoryId", (object?)categoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("status", (int)status);
        command.Parameters.AddWithValue("publishedAt",
            status == ArticleStatus.Draft ? DBNull.Value : publishedAt ?? DateTime.UtcNow.AddMinutes(-1));
        command.Parameters.AddWithValue("content", content);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task AddVersionAsync(int articleId, int versionNumber, string content)
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(@"
            INSERT INTO article_contents (id, article_id, content, version_number, created_at)
            VALUES (@articleId * 10 + @version, @articleId, @content, @version, now())");
        command.Parameters.AddWithValue("articleId", articleId);
        command.Parameters.AddWithValue("version", versionNumber);
        command.Parameters.AddWithValue("content", content);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteTestArticlesAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(
            $"DELETE FROM articles WHERE id >= {FirstTestArticleId} AND id < {FirstTestArticleId + 1000}");
        await command.ExecuteNonQueryAsync();
    }
}
