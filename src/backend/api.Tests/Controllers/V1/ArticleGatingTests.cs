using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class ArticleGatingTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public ArticleGatingTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetGatedArticle_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.GatedArticleSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("SIGN_IN_REQUIRED");
    }

    [Fact]
    public async Task GetGatedArticle_WithWebsiteAuth_ReturnsArticle()
    {
        var client = _fixture.Factory.CreateClient();
        AuthHelper.CreateWebsiteUserAuthenticatedClient(client);

        var response = await client.GetAsync($"/api/v1/articles/{TestSeedData.GatedArticleSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.GatedArticleSlug);
        result.Data.RequiresSignIn.Should().BeTrue();
    }

    [Fact]
    public async Task GetNonGatedArticle_WithoutAuth_ReturnsArticle()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.PublishedArticle1Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.PublishedArticle1Slug);
        result.Data.RequiresSignIn.Should().BeFalse();
    }

    [Fact]
    public async Task GetGatedArticle_WithAdminAuth_ReturnsUnauthorized()
    {
        // Admin tokens don't set WebsiteUserId, so gated articles should still be blocked
        var client = _fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(client);

        var response = await client.GetAsync($"/api/v1/articles/{TestSeedData.GatedArticleSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("SIGN_IN_REQUIRED");
    }

    [Fact]
    public async Task GetArticles_ListIncludesGatedArticle()
    {
        // The article list should still include gated articles (just their metadata)
        var response = await _client.GetAsync("/api/v1/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(a => a.Slug == TestSeedData.GatedArticleSlug);
    }

    // ---- pages and comments are gated like articles ----

    private async Task<(int Id, string Slug)> CreateGatedPageAsync()
    {
        var admin = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient());
        var slug = $"gated-page-{Guid.NewGuid():N}";
        var response = await admin.PostAsJsonAsync("/api/admin/pages", new
        {
            Title = "Gated page",
            Slug = slug,
            Content = "members only",
            Status = SharpQuill.Core.Articles.ArticleStatus.Published,
            RequiresSignIn = true
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        return (created!.Data.Id, slug);
    }

    [Fact]
    public async Task GetGatedPage_WithoutAuth_ReturnsUnauthorized_AndWithAuthReturnsPage()
    {
        var (id, slug) = await CreateGatedPageAsync();

        var anonymous = await _client.GetAsync($"/api/v1/pages/{slug}");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var member = _fixture.Factory.CreateClient();
        AuthHelper.CreateWebsiteUserAuthenticatedClient(member);
        var authed = await member.GetAsync($"/api/v1/pages/{slug}");
        authed.StatusCode.Should().Be(HttpStatusCode.OK);

        var admin = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient());
        await admin.DeleteAsync($"/api/admin/pages/{id}");
    }

    [Fact]
    public async Task GetCommentsOfGatedArticle_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.GatedArticleId}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCommentsOfGatedArticle_WithWebsiteAuth_ReturnsOk()
    {
        var member = _fixture.Factory.CreateClient();
        AuthHelper.CreateWebsiteUserAuthenticatedClient(member);

        var response = await member.GetAsync($"/api/v1/articles/{TestSeedData.GatedArticleId}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostCommentOnGatedArticle_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync($"/api/v1/articles/{TestSeedData.GatedArticleId}/comments",
            new { AuthorName = "Anon", AuthorEmail = "anon@example.com", Content = "hi" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCommentsOfPublicArticle_WithoutAuth_StillOk()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.PublishedArticle1Id}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
