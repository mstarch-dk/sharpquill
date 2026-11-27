using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Series.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminSeriesControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminSeriesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetSeries_ReturnsAllSeries()
    {
        var response = await _client.GetAsync("/api/admin/series");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SeriesDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(s => s.Slug == TestSeedData.GettingStartedSeriesSlug);
    }

    [Fact]
    public async Task GetSeries_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/series");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSeriesById_WithValidId_ReturnsSeries()
    {
        var response = await _client.GetAsync($"/api/admin/series/{TestSeedData.GettingStartedSeriesId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeriesAdminDetailDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.GettingStartedSeriesSlug);
        result.Data.Articles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSeriesById_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/series/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSeries_WithValidData_ReturnsCreated()
    {
        var uniqueSlug = $"test-series-{Guid.NewGuid():N}";
        var dto = new { Name = "Test Series", Slug = uniqueSlug, Description = "A test series" };

        var response = await _client.PostAsJsonAsync("/api/admin/series", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        result.Should().NotBeNull();
        result!.Data.Name.Should().Be("Test Series");
        result.Data.Slug.Should().Be(uniqueSlug);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/series/{result.Data.Id}");
    }

    [Fact]
    public async Task UpdateSeries_WithValidData_ReturnsUpdated()
    {
        var uniqueSlug = $"test-series-upd-{Guid.NewGuid():N}";
        var createDto = new { Name = "Original Series", Slug = uniqueSlug, Description = "Desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/series", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        var id = created!.Data.Id;

        var updateDto = new { Name = "Updated Series", Slug = uniqueSlug, Description = "Updated desc" };
        var response = await _client.PutAsJsonAsync($"/api/admin/series/{id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        result!.Data.Name.Should().Be("Updated Series");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/series/{id}");
    }

    [Fact]
    public async Task DeleteSeries_WithValidId_ReturnsNoContent()
    {
        var uniqueSlug = $"test-series-del-{Guid.NewGuid():N}";
        var dto = new { Name = "To Delete", Slug = uniqueSlug };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/series", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        var id = created!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/series/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddArticle_ToSeries_ReturnsOk()
    {
        // Create a test series and article
        var seriesSlug = $"test-series-add-{Guid.NewGuid():N}";
        var seriesResponse = await _client.PostAsJsonAsync("/api/admin/series", new { Name = "Add Test", Slug = seriesSlug });
        var series = await seriesResponse.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        var seriesId = series!.Data.Id;

        var articleSlug = $"test-article-add-{Guid.NewGuid():N}";
        var articleResponse = await _client.PostAsJsonAsync("/api/admin/articles", new { Title = "Add Test Article", Slug = articleSlug, Content = "Content", Status = 0 });
        var article = await articleResponse.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        var articleId = (int)article!.Data.GetProperty("id").GetInt32();

        // Add article to series
        var response = await _client.PostAsJsonAsync($"/api/admin/series/{seriesId}/articles", new { ArticleId = articleId, Position = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/series/{seriesId}/articles/{articleId}");
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
        await _client.DeleteAsync($"/api/admin/series/{seriesId}");
    }

    [Fact]
    public async Task RemoveArticle_FromSeries_ReturnsNoContent()
    {
        // Create a test series and article
        var seriesSlug = $"test-series-rm-{Guid.NewGuid():N}";
        var seriesResponse = await _client.PostAsJsonAsync("/api/admin/series", new { Name = "Remove Test", Slug = seriesSlug });
        var series = await seriesResponse.Content.ReadFromJsonAsync<ApiResponse<SeriesDto>>();
        var seriesId = series!.Data.Id;

        var articleSlug = $"test-article-rm-{Guid.NewGuid():N}";
        var articleResponse = await _client.PostAsJsonAsync("/api/admin/articles", new { Title = "Remove Test Article", Slug = articleSlug, Content = "Content", Status = 0 });
        var article = await articleResponse.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        var articleId = (int)article!.Data.GetProperty("id").GetInt32();

        // Add then remove
        await _client.PostAsJsonAsync($"/api/admin/series/{seriesId}/articles", new { ArticleId = articleId, Position = 1 });
        var response = await _client.DeleteAsync($"/api/admin/series/{seriesId}/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
        await _client.DeleteAsync($"/api/admin/series/{seriesId}");
    }

    [Fact]
    public async Task ReorderArticles_WithValidData_ReturnsOk()
    {
        // Use seed data - Getting Started series has articles 1 and 2
        var dto = new { ArticleIds = new[] { TestSeedData.PublishedArticle2Id, TestSeedData.PublishedArticle1Id } };

        var response = await _client.PutAsJsonAsync($"/api/admin/series/{TestSeedData.GettingStartedSeriesId}/articles/reorder", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original order
        var restoreDto = new { ArticleIds = new[] { TestSeedData.PublishedArticle1Id, TestSeedData.PublishedArticle2Id } };
        await _client.PutAsJsonAsync($"/api/admin/series/{TestSeedData.GettingStartedSeriesId}/articles/reorder", restoreDto);
    }
}
