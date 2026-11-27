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
/// GET /api/v1/search. Every test searches for made-up words that only its own articles contain, so the seed data and
/// other test classes do not affect the results. Articles are written with SQL (ids from 9000), which also exercises
/// the database triggers that maintain articles.search_vector.
/// </summary>
[Collection("Database")]
public class SearchControllerTests : IAsyncLifetime
{
    private const int FirstTestArticleId = 9000;

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private int _nextArticleId = FirstTestArticleId;

    public SearchControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => DeleteTestArticlesAsync();

    public Task DisposeAsync() => DeleteTestArticlesAsync();

    [Theory]
    [InlineData("")]
    [InlineData("q=")]
    [InlineData("q=%20%20%20")]
    public async Task Search_WithBlankQuery_ReturnsEmptyPage(string queryString)
    {
        var result = await SearchAsync(queryString);

        result.Data.Should().BeEmpty();
        Meta(result).GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Search_RanksTitleMatchAboveBodyMatch()
    {
        var bodyMatch = await InsertArticleAsync("Plain title", "Somewhere in the body: zorbleflux appears once.");
        var titleMatch = await InsertArticleAsync("All about zorbleflux", "Nothing else to say.");

        var result = await SearchAsync("q=zorbleflux");

        result.Data.Select(r => r.Article.Id).Should().Equal(titleMatch, bodyMatch);
        Meta(result).GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Search_ReturnsSnippetWithMatchedWordsMarked()
    {
        await InsertArticleAsync("Snippet test",
            "The opening sentence is ordinary. Then the **quindarvok** shows up in bold, with a [link](https://example.com/x).");

        var result = await SearchAsync("q=quindarvok");

        var hit = result.Data.Should().ContainSingle().Subject;
        hit.Snippet.Where(p => p.Match).Select(p => p.Text).Should().Equal("quindarvok");
        var text = string.Concat(hit.Snippet.Select(p => p.Text));
        text.Should().Contain("Then the quindarvok shows up in bold, with a link");
        text.Should().NotContain("**").And.NotContain("https://").And.NotContain("\u0002").And.NotContain("\u0003");
    }

    [Fact]
    public async Task Search_ReturnsArticleListFields()
    {
        var id = await InsertArticleAsync("Field check vexmorrin", "Body", excerpt: "A short excerpt");

        var result = await SearchAsync("q=vexmorrin");

        var article = result.Data.Should().ContainSingle().Subject.Article;
        article.Id.Should().Be(id);
        article.Slug.Should().Be($"search-test-{id}");
        article.Title.Should().Be("Field check vexmorrin");
        article.Excerpt.Should().Be("A short excerpt");
        article.RequiresSignIn.Should().BeFalse();
    }

    [Fact]
    public async Task Search_MatchesExcerpt()
    {
        var id = await InsertArticleAsync("Excerpt only", "Body text", excerpt: "Mentions plorvantic here");

        var result = await SearchAsync("q=plorvantic");

        result.Data.Select(r => r.Article.Id).Should().Equal(id);
    }

    [Fact]
    public async Task Search_ReturnsOnlyPublishedArticles()
    {
        var published = await InsertArticleAsync("Published glimmerquat", "Body");
        await InsertArticleAsync("Draft glimmerquat", "Body", status: ArticleStatus.Draft);
        await InsertArticleAsync("Archived glimmerquat", "Body", status: ArticleStatus.Archived);
        await InsertArticleAsync("Page glimmerquat", "Body", contentType: ContentType.Page);
        // A draft that never had a published version: only the title would be indexed, and the status still hides it.
        await InsertArticleAsync("Never published glimmerquat", "Body", status: ArticleStatus.Draft, publish: false);

        var result = await SearchAsync("q=glimmerquat");

        result.Data.Select(r => r.Article.Id).Should().Equal(published);
    }

    [Fact]
    public async Task Search_ScheduledArticles_StayHiddenAndUncountedUntilTheirPublishTime()
    {
        var live = await InsertArticleAsync("Live quillobrant", "Body");
        var scheduled = await InsertArticleAsync("Scheduled quillobrant", "Body", publishedAt: DateTime.UtcNow.AddDays(1));

        var before = await SearchAsync("q=quillobrant");
        before.Data.Select(r => r.Article.Id).Should().Equal(live);
        Meta(before).GetProperty("totalCount").GetInt32().Should().Be(1);

        await ExecuteAsync($"UPDATE articles SET published_at = now() - interval '1 minute' WHERE id = {scheduled}");

        var after = await SearchAsync("q=quillobrant");
        after.Data.Select(r => r.Article.Id).Should().BeEquivalentTo([live, scheduled]);
        Meta(after).GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Search_MembersOnlyArticles_AreHiddenFromAnonymousReaders()
    {
        var open = await InsertArticleAsync("Open snorkelvine", "Body");
        await InsertArticleAsync("Members snorkelvine", "Body", requiresSignIn: true);

        var result = await SearchAsync("q=snorkelvine");

        result.Data.Select(r => r.Article.Id).Should().Equal(open);
        Meta(result).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_MembersOnlyArticles_AreShownToSignedInReaders()
    {
        var open = await InsertArticleAsync("Open snorkelvine", "Body");
        var members = await InsertArticleAsync("Members snorkelvine", "Body", requiresSignIn: true);
        var client = AuthHelper.CreateWebsiteUserAuthenticatedClient(_fixture.Factory.CreateClient());

        var result = await SearchAsync("q=snorkelvine", client);

        result.Data.Select(r => r.Article.Id).Should().BeEquivalentTo([open, members]);
        result.Data.Single(r => r.Article.Id == members).Article.RequiresSignIn.Should().BeTrue();
    }

    [Fact]
    public async Task Search_MembersOnlyArticles_AreHiddenFromAdminTokens()
    {
        // Same rule as GET /api/v1/articles/{slug}: only a website-reader token unlocks members-only content.
        var open = await InsertArticleAsync("Open snorkelvine", "Body");
        await InsertArticleAsync("Members snorkelvine", "Body", requiresSignIn: true);
        var client = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient());

        var result = await SearchAsync("q=snorkelvine", client);

        result.Data.Select(r => r.Article.Id).Should().Equal(open);
    }

    [Theory]
    [InlineData("\"unclosed quote")]
    [InlineData("-")]
    [InlineData("or")]
    [InlineData("!!! &|:* ( ) <-> \\")]
    [InlineData("'); DROP TABLE articles; --")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("og det er")]
    public async Task Search_WithAnyInput_DoesNotFail(string query)
    {
        var response = await _client.GetAsync($"/api/v1/search?q={Uri.EscapeDataString(query)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var articles = await _client.GetAsync("/api/v1/articles");
        articles.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_WithVeryLongQuery_DoesNotFail()
    {
        var response = await _client.GetAsync($"/api/v1/search?q={new string('a', 5000)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_SupportsWebSearchSyntax()
    {
        var both = await InsertArticleAsync("Trellawind and brosquee", "Body");
        var onlyFirst = await InsertArticleAsync("Trellawind alone", "Body");

        var excluded = await SearchAsync("q=" + Uri.EscapeDataString("trellawind -brosquee"));
        var either = await SearchAsync("q=" + Uri.EscapeDataString("brosquee or alone"));
        var phrase = await SearchAsync("q=" + Uri.EscapeDataString("\"trellawind alone\""));

        excluded.Data.Select(r => r.Article.Id).Should().Equal(onlyFirst);
        either.Data.Select(r => r.Article.Id).Should().BeEquivalentTo([both, onlyFirst]);
        phrase.Data.Select(r => r.Article.Id).Should().Equal(onlyFirst);
    }

    [Fact]
    public async Task Search_IsPaginated()
    {
        for (var i = 0; i < 3; i++)
        {
            await InsertArticleAsync($"Pagination fennelwisp {i}", "Body", publishedAt: new DateTime(2025, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc));
        }

        var page1 = await SearchAsync("q=fennelwisp&page=1&pageSize=2");
        var page2 = await SearchAsync("q=fennelwisp&page=2&pageSize=2");
        var beyond = await SearchAsync("q=fennelwisp&page=5&pageSize=2");

        page1.Data.Should().HaveCount(2);
        page2.Data.Should().HaveCount(1);
        beyond.Data.Should().BeEmpty();
        page1.Data.Concat(page2.Data).Select(r => r.Article.Id).Should().OnlyHaveUniqueItems();
        foreach (var page in new[] { page1, page2, beyond })
        {
            Meta(page).GetProperty("totalCount").GetInt32().Should().Be(3);
            Meta(page).GetProperty("totalPages").GetInt32().Should().Be(2);
            Meta(page).GetProperty("pageSize").GetInt32().Should().Be(2);
        }
        Meta(page2).GetProperty("page").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Search_FindsTextInBlockLayouts_ButNotUrls()
    {
        const string layout = """
            {"version":1,"sections":[{"type":"full","blocks":[
              {"type":"text","content":"Layout paragraph about ombrelquist."},
              {"type":"cards","heading":"Cards","items":[{"title":"Card","description":"Card text mentions trevelstone"}]},
              {"type":"cta","buttonText":"Join","buttonUrl":"https://hostnamewordzz.example/signup"}
            ]}]}
            """;
        var id = await InsertArticleAsync("Block layout article", content: null, layout: layout);

        (await SearchAsync("q=ombrelquist")).Data.Select(r => r.Article.Id).Should().Equal(id);
        (await SearchAsync("q=trevelstone")).Data.Select(r => r.Article.Id).Should().Equal(id);
        (await SearchAsync("q=hostnamewordzz")).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_UsesStemmingForTheSiteLanguage()
    {
        // No language setting means Danish (the web app's default): "bil" finds "bilerne".
        var id = await InsertArticleAsync("Om bilerne i grimbelstad", "Tekst");

        (await SearchAsync("q=bil")).Data.Select(r => r.Article.Id).Should().Contain(id);
    }

    [Fact]
    public async Task Search_AfterLanguageChange_RebuildsTheIndex()
    {
        var id = await InsertArticleAsync("Wobblecrest running notes", "Body");

        // Danish stemming keeps "running"; the query "runs" does not match it.
        (await SearchAsync("q=wobblecrest%20runs")).Data.Should().BeEmpty();

        try
        {
            await ExecuteAsync("INSERT INTO site_settings (key, value) VALUES ('language', 'en-GB') " +
                               "ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value");

            // English stemming: "running" and "runs" are both "run". Only a rebuilt vector can match.
            (await SearchAsync("q=wobblecrest%20runs")).Data.Select(r => r.Article.Id).Should().Equal(id);
        }
        finally
        {
            await ExecuteAsync("DELETE FROM site_settings WHERE key = 'language'");
        }

        (await SearchAsync("q=wobblecrest%20runs")).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FollowsTitleChangesAndUnpublishing()
    {
        var id = await InsertArticleAsync("Original crandlemoor", "Body");

        await ExecuteAsync($"UPDATE articles SET title = 'Renamed sprockettine' WHERE id = {id}");
        (await SearchAsync("q=crandlemoor")).Data.Should().BeEmpty();
        (await SearchAsync("q=sprockettine")).Data.Select(r => r.Article.Id).Should().Equal(id);

        await ExecuteAsync($"UPDATE articles SET status = {(int)ArticleStatus.Draft} WHERE id = {id}");
        (await SearchAsync("q=sprockettine")).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FindsArticlePublishedThroughTheAdminApi()
    {
        var admin = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient());
        var slug = $"search-admin-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Admin published article",
            Slug = slug,
            Content = "Written in the editor about zingleberry.",
            Status = ArticleStatus.Published
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();

        try
        {
            (await SearchAsync("q=zingleberry")).Data.Select(r => r.Article.Slug).Should().Equal(slug);
        }
        finally
        {
            await admin.DeleteAsync($"/api/admin/articles/{created!.Data.Id}");
        }

        (await SearchAsync("q=zingleberry")).Data.Should().BeEmpty();
    }

    private async Task<ApiResponse<List<SearchResultDto>>> SearchAsync(string queryString, HttpClient? client = null)
    {
        var response = await (client ?? _client).GetAsync($"/api/v1/search?{queryString}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SearchResultDto>>>();
        result.Should().NotBeNull();
        return result!;
    }

    private static System.Text.Json.JsonElement Meta(ApiResponse<List<SearchResultDto>> result) =>
        result.Meta.Should().BeOfType<System.Text.Json.JsonElement>().Subject;

    private async Task<int> InsertArticleAsync(
        string title,
        string? content,
        string? excerpt = null,
        string? layout = null,
        ArticleStatus status = ArticleStatus.Published,
        ContentType contentType = ContentType.Article,
        bool requiresSignIn = false,
        bool publish = true,
        DateTime? publishedAt = null)
    {
        var id = _nextArticleId++;
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(@"
            INSERT INTO articles (id, title, slug, content_type, author_id, status, requires_sign_in, published_at, created_at)
            VALUES (@id, @title, @slug, @contentType, @authorId, @status, @requiresSignIn, @publishedAt, now());
            INSERT INTO article_contents (id, article_id, content, excerpt, layout, version_number, created_at)
            VALUES (@id, @id, @content, @excerpt, @layout::jsonb, 1, now());
            UPDATE articles SET published_version_id = CASE WHEN @publish THEN @id END WHERE id = @id;");
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("slug", $"search-test-{id}");
        command.Parameters.AddWithValue("contentType", (int)contentType);
        command.Parameters.AddWithValue("authorId", TestSeedData.AdminAuthorId);
        command.Parameters.AddWithValue("status", (int)status);
        command.Parameters.AddWithValue("requiresSignIn", requiresSignIn);
        command.Parameters.AddWithValue("publishedAt", publishedAt ?? DateTime.UtcNow);
        command.Parameters.AddWithValue("content", (object?)content ?? DBNull.Value);
        command.Parameters.AddWithValue("excerpt", (object?)excerpt ?? DBNull.Value);
        command.Parameters.AddWithValue("layout", (object?)layout ?? DBNull.Value);
        command.Parameters.AddWithValue("publish", publish);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private static Task DeleteTestArticlesAsync() =>
        ExecuteAsync($"DELETE FROM articles WHERE id >= {FirstTestArticleId} AND id < {FirstTestArticleId + 1000}");
}
