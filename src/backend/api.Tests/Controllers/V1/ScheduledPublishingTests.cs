using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Application.Series.DTOs;
using SharpQuill.Application.Tags.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

/// <summary>
/// Scheduled publishing (TASK-91, public_articles view): a Published article or page whose published_at lies in the future is hidden from
/// every public endpoint until the database clock passes that time, then appears without any further step.
/// Each test seeds its own category, tag, series, navigation and content (unique slugs) and removes it afterwards,
/// so the shared seed data and other tests' counts are untouched.
/// </summary>
[Collection("Database")]
public class ScheduledPublishingTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..10];

    private int _categoryId;
    private int _tagId;
    private int _seriesId;
    private int _navigationId;
    private int _liveArticleId;
    private int _scheduledArticleId;
    private int _scheduledPageId;

    private string CategorySlug => $"sched-cat-{_suffix}";
    private string TagSlug => $"sched-tag-{_suffix}";
    private string SeriesSlug => $"sched-series-{_suffix}";
    private string NavigationSlug => $"sched-nav-{_suffix}";
    private string LiveSlug => $"sched-live-{_suffix}";
    private string ScheduledSlug => $"sched-future-{_suffix}";
    private string PageSlug => $"sched-page-{_suffix}";

    public ScheduledPublishingTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var conn = await OpenAsync();

        _categoryId = await InsertAsync(conn,
            "INSERT INTO categories (name, slug, created_at) VALUES (@n, @s, now()) RETURNING id",
            ("@n", $"Sched cat {_suffix}"), ("@s", CategorySlug));
        _tagId = await InsertAsync(conn,
            "INSERT INTO tags (name, slug, created_at) VALUES (@n, @s, now()) RETURNING id",
            ("@n", $"Sched tag {_suffix}"), ("@s", TagSlug));
        _seriesId = await InsertAsync(conn,
            "INSERT INTO series (name, slug, created_at) VALUES (@n, @s, now()) RETURNING id",
            ("@n", $"Sched series {_suffix}"), ("@s", SeriesSlug));

        // A live article (published an hour ago) and a scheduled one (goes live tomorrow) in the same
        // category, tag and series, so lists, counts and series navigation can be compared.
        _liveArticleId = await InsertArticleAsync(conn, "Live", LiveSlug, ContentType.Article, seriesOrder: 1, "now() - interval '1 hour'");
        _scheduledArticleId = await InsertArticleAsync(conn, "Scheduled", ScheduledSlug, ContentType.Article, seriesOrder: 2, "now() + interval '1 day'");
        _scheduledPageId = await InsertArticleAsync(conn, "Scheduled page", PageSlug, ContentType.Page, seriesOrder: null, "now() + interval '1 day'");

        _navigationId = await InsertAsync(conn,
            "INSERT INTO navigations (name, slug) VALUES (@n, @s) RETURNING id",
            ("@n", $"Sched nav {_suffix}"), ("@s", NavigationSlug));
        await InsertAsync(conn,
            @"INSERT INTO navigation_items (navigation_id, label, item_type, article_id, position, open_in_new_tab)
              VALUES (@nav, 'Scheduled page', 0, @page, 1, false) RETURNING id",
            ("@nav", _navigationId), ("@page", _scheduledPageId));
    }

    public async Task DisposeAsync()
    {
        await using var conn = await OpenAsync();
        var ids = new[] { _liveArticleId, _scheduledArticleId, _scheduledPageId };
        await using var cmd = new NpgsqlCommand(@"
            DELETE FROM navigation_items WHERE navigation_id = @nav;
            DELETE FROM navigations WHERE id = @nav;
            DELETE FROM article_tags WHERE article_id = ANY(@ids);
            UPDATE articles SET published_version_id = NULL WHERE id = ANY(@ids);
            DELETE FROM article_contents WHERE article_id = ANY(@ids);
            DELETE FROM articles WHERE id = ANY(@ids);
            DELETE FROM series WHERE id = @series;
            DELETE FROM tags WHERE id = @tag;
            DELETE FROM categories WHERE id = @cat;", conn);
        cmd.Parameters.AddWithValue("@nav", _navigationId);
        cmd.Parameters.AddWithValue("@ids", ids);
        cmd.Parameters.AddWithValue("@series", _seriesId);
        cmd.Parameters.AddWithValue("@tag", _tagId);
        cmd.Parameters.AddWithValue("@cat", _categoryId);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task ScheduledArticle_IsHiddenFromEveryPublicEndpoint_BeforeItsPublishTime()
    {
        (await _client.GetAsync($"/api/v1/articles/{ScheduledSlug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/v1/pages/{PageSlug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await GetListAsync("/api/v1/articles?pageSize=100")).Should().NotContain(a => a.Slug == ScheduledSlug).And.Contain(a => a.Slug == LiveSlug);
        (await GetListAsync("/api/v1/articles/featured")).Should().NotContain(a => a.Slug == ScheduledSlug);

        var byCategory = await GetPagedAsync($"/api/v1/categories/{CategorySlug}/articles");
        byCategory.Data.Select(a => a.Slug).Should().Equal(LiveSlug);
        byCategory.Meta.TotalCount.Should().Be(1);

        var byTag = await GetPagedAsync($"/api/v1/tags/{TagSlug}/articles");
        byTag.Data.Select(a => a.Slug).Should().Equal(LiveSlug);
        byTag.Meta.TotalCount.Should().Be(1);

        (await GetAsync<IReadOnlyList<CategoryDto>>("/api/v1/categories")).Single(c => c.Slug == CategorySlug).ArticleCount.Should().Be(1);
        (await GetAsync<IReadOnlyList<TagDto>>("/api/v1/tags")).Single(t => t.Slug == TagSlug).ArticleCount.Should().Be(1);
        (await GetAsync<IReadOnlyList<SeriesDto>>("/api/v1/series")).Single(s => s.Slug == SeriesSlug).ArticleCount.Should().Be(1);
        (await GetAsync<SeriesDetailDto>($"/api/v1/series/{SeriesSlug}")).Articles.Select(a => a.Slug).Should().Equal(LiveSlug);

        // Series navigation on the live article must not point at the scheduled one
        var live = await GetAsync<ArticleDto>($"/api/v1/articles/{LiveSlug}");
        live.Series.Should().NotBeNull();
        live.Series!.TotalInSeries.Should().Be(1);
        live.Series.Next.Should().BeNull();

        // A menu item for a scheduled page does not reveal its slug
        (await GetAsync<NavigationDto>($"/api/v1/navigations/{NavigationSlug}")).Items.Single().Url.Should().Be("#");

        // No comments on an article that is not out yet
        var comment = await _client.PostAsJsonAsync($"/api/v1/articles/{_scheduledArticleId}/comments",
            new { AuthorName = "Reader", AuthorEmail = "reader@example.com", Content = "Too early", ParentCommentId = (int?)null, Honeypot = (string?)null });
        comment.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ScheduledArticle_AppearsEverywhere_OncePublishTimeHasPassed()
    {
        // Simulate the clock passing the publish time: no status change or other manual step
        await using (var conn = await OpenAsync())
        await using (var cmd = new NpgsqlCommand(
            "UPDATE articles SET published_at = now() - interval '1 minute' WHERE id = ANY(@ids)", conn))
        {
            cmd.Parameters.AddWithValue("@ids", new[] { _scheduledArticleId, _scheduledPageId });
            await cmd.ExecuteNonQueryAsync();
        }

        (await _client.GetAsync($"/api/v1/articles/{ScheduledSlug}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync($"/api/v1/pages/{PageSlug}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetListAsync("/api/v1/articles?pageSize=100")).Should().Contain(a => a.Slug == ScheduledSlug);

        (await GetPagedAsync($"/api/v1/categories/{CategorySlug}/articles")).Meta.TotalCount.Should().Be(2);
        (await GetPagedAsync($"/api/v1/tags/{TagSlug}/articles")).Meta.TotalCount.Should().Be(2);
        (await GetAsync<IReadOnlyList<CategoryDto>>("/api/v1/categories")).Single(c => c.Slug == CategorySlug).ArticleCount.Should().Be(2);
        (await GetAsync<IReadOnlyList<TagDto>>("/api/v1/tags")).Single(t => t.Slug == TagSlug).ArticleCount.Should().Be(2);
        (await GetAsync<IReadOnlyList<SeriesDto>>("/api/v1/series")).Single(s => s.Slug == SeriesSlug).ArticleCount.Should().Be(2);
        (await GetAsync<SeriesDetailDto>($"/api/v1/series/{SeriesSlug}")).Articles.Select(a => a.Slug).Should().Equal(LiveSlug, ScheduledSlug);

        var live = await GetAsync<ArticleDto>($"/api/v1/articles/{LiveSlug}");
        live.Series!.TotalInSeries.Should().Be(2);
        live.Series.Next!.Slug.Should().Be(ScheduledSlug);

        (await GetAsync<NavigationDto>($"/api/v1/navigations/{NavigationSlug}")).Items.Single().Url.Should().Be($"/{PageSlug}");
    }

    [Fact]
    public async Task PublishedArticleWithoutPublishDate_StaysVisible()
    {
        await using (var conn = await OpenAsync())
        await using (var cmd = new NpgsqlCommand("UPDATE articles SET published_at = NULL WHERE id = @id", conn))
        {
            cmd.Parameters.AddWithValue("@id", _liveArticleId);
            await cmd.ExecuteNonQueryAsync();
        }

        (await _client.GetAsync($"/api/v1/articles/{LiveSlug}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminCreate_WithFuturePublishDate_KeepsTheDateAndHidesTheArticle()
    {
        var admin = _fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(admin);
        var publishAt = DateTime.UtcNow.AddDays(3);
        var slug = $"sched-admin-{_suffix}";

        var create = await admin.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Scheduled via admin",
            Slug = slug,
            Content = "Coming soon",
            Status = ArticleStatus.Published,
            PublishedAt = publishAt
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data;

        try
        {
            created.Status.Should().Be(ArticleStatus.Published);
            created.PublishedAt.Should().BeCloseTo(publishAt, TimeSpan.FromSeconds(1));

            // The admin still sees it; the public site does not
            (await admin.GetAsync($"/api/admin/articles/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await _client.GetAsync($"/api/v1/articles/{slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            (await admin.DeleteAsync($"/api/admin/articles/{created.Id}")).IsSuccessStatusCode.Should().BeTrue();
        }
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, url);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>())!.Data;
    }

    private Task<IReadOnlyList<ArticleListDto>> GetListAsync(string url) => GetAsync<IReadOnlyList<ArticleListDto>>(url);

    private async Task<PagedResultDto<ArticleListDto>> GetPagedAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, url);
        return (await response.Content.ReadFromJsonAsync<PagedResultDto<ArticleListDto>>())!;
    }

    private static async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(DatabaseFixture.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task<int> InsertAsync(NpgsqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> InsertArticleAsync(NpgsqlConnection conn, string title, string slug, ContentType type, int? seriesOrder, string publishedAtSql)
    {
        var isArticle = type == ContentType.Article;
        var id = await InsertAsync(conn, $@"
            INSERT INTO articles (title, slug, content_type, author_id, category_id, series_id, series_order, status,
                                  requires_sign_in, comments_enabled, published_at, created_at)
            VALUES (@title, @slug, @type, {TestSeedData.AdminAuthorId}, @cat, @series, @order, {(int)ArticleStatus.Published},
                    false, true, {publishedAtSql}, now())
            RETURNING id",
            ("@title", title), ("@slug", slug), ("@type", (int)type),
            ("@cat", isArticle ? _categoryId : DBNull.Value),
            ("@series", isArticle ? _seriesId : DBNull.Value),
            ("@order", (object?)seriesOrder ?? DBNull.Value));

        var versionId = await InsertAsync(conn,
            "INSERT INTO article_contents (article_id, content, version_number, created_at) VALUES (@a, 'Body', 1, now()) RETURNING id",
            ("@a", id));
        await InsertAsync(conn, "UPDATE articles SET published_version_id = @v WHERE id = @a RETURNING id", ("@v", versionId), ("@a", id));

        if (isArticle)
            await InsertAsync(conn, "INSERT INTO article_tags (article_id, tag_id) VALUES (@a, @t) RETURNING article_id", ("@a", id), ("@t", _tagId));

        return id;
    }
}
