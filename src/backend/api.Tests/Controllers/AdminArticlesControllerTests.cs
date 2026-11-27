using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminArticlesControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminArticlesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetArticles_WithValidToken_ReturnsArticlesList()
    {
        var response = await _client.GetAsync("/api/admin/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleAdminDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetArticles_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetArticles_SupportsPagination()
    {
        var response = await _client.GetAsync("/api/admin/articles?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleAdminDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task CreateArticle_WithValidData_ReturnsCreatedArticle()
    {
        var uniqueSlug = $"test-article-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Test Article",
            Slug = uniqueSlug,
            Content = "Test content",
            Excerpt = "Test excerpt",
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Title.Should().Be("Test Article");
        result.Data.Slug.Should().Be(uniqueSlug);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{result.Data.Id}");
    }

    [Fact]
    public async Task CreateArticle_WithInvalidSlug_ReturnsBadRequest()
    {
        var createDto = new
        {
            Title = "Test Article",
            Slug = "Invalid Slug With Spaces",
            Content = "Test content"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetArticle_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/articles/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateArticle_WithValidData_ReturnsUpdatedArticle()
    {
        var uniqueSlug = $"test-update-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Original Title",
            Slug = uniqueSlug,
            Content = "Original content",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        var updateDto = new
        {
            Title = "Updated Title",
            Slug = uniqueSlug,
            Content = "Updated content",
            Status = ArticleStatus.Published
        };

        var response = await _client.PutAsJsonAsync($"/api/admin/articles/{articleId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Title.Should().Be("Updated Title");
        result.Data.Status.Should().Be(ArticleStatus.Published);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task DeleteArticle_WithValidId_ReturnsNoContent()
    {
        var uniqueSlug = $"test-delete-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Article to Delete",
            Slug = uniqueSlug,
            Content = "Content",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/admin/articles/{articleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPreview_WithPublishedArticle_ReturnsFullArticleDto()
    {
        var response = await _client.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(TestSeedData.PublishedArticle1Id);
        result.Data.Title.Should().Be("Intro to C#");
        result.Data.Slug.Should().Be(TestSeedData.PublishedArticle1Slug);
        result.Data.Content.Should().NotBeNullOrEmpty();
        result.Data.Excerpt.Should().Be("Learn C# basics");
        result.Data.PublishedAt.Should().NotBeNull();
        result.Data.Author.Should().NotBeNull();
        result.Data.Author!.Name.Should().Be("Admin");
        result.Data.Category.Should().NotBeNull();
        result.Data.Category!.Name.Should().Be("Technology");
        result.Data.Tags.Should().HaveCount(3);
        result.Data.Layout.Should().BeNull();
        result.Data.Series.Should().BeNull();
    }

    [Fact]
    public async Task GetPreview_WithDraftArticle_ReturnsArticleDto()
    {
        var response = await _client.GetAsync($"/api/admin/articles/{TestSeedData.DraftArticleId}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(TestSeedData.DraftArticleId);
        result.Data.PublishedAt.Should().BeNull();
        result.Data.Tags.Should().BeEmpty();
        result.Data.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPreview_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/articles/99999/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPreview_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
