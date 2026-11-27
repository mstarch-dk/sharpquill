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
public class AdminPagesControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminPagesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetPages_ReturnsAllPages()
    {
        var response = await _client.GetAsync("/api/admin/pages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleAdminDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(p => p.Slug == TestSeedData.HjemPageSlug);
    }

    [Fact]
    public async Task GetPages_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/pages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPage_WithValidId_ReturnsPage()
    {
        var response = await _client.GetAsync($"/api/admin/pages/{TestSeedData.HjemPageId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.HjemPageSlug);
    }

    [Fact]
    public async Task GetPage_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/pages/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePage_WithValidData_ReturnsCreated()
    {
        var uniqueSlug = $"test-page-{Guid.NewGuid():N}";
        var dto = new
        {
            Title = "Test Page",
            Slug = uniqueSlug,
            Content = "# Test\nTest content",
            Excerpt = "Test excerpt",
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/pages", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Title.Should().Be("Test Page");
        result.Data.Slug.Should().Be(uniqueSlug);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/pages/{result.Data.Id}");
    }

    [Fact]
    public async Task UpdatePage_WithValidData_ReturnsUpdated()
    {
        var uniqueSlug = $"test-page-upd-{Guid.NewGuid():N}";
        var createDto = new { Title = "Original", Slug = uniqueSlug, Content = "Original content", Status = ArticleStatus.Draft };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/pages", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var id = created!.Data.Id;

        var updateDto = new { Title = "Updated Page", Slug = uniqueSlug, Content = "Updated content", Status = ArticleStatus.Published };
        var response = await _client.PutAsJsonAsync($"/api/admin/pages/{id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result!.Data.Title.Should().Be("Updated Page");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/pages/{id}");
    }

    [Fact]
    public async Task DeletePage_WithValidId_ReturnsNoContent()
    {
        var uniqueSlug = $"test-page-del-{Guid.NewGuid():N}";
        var dto = new { Title = "To Delete", Slug = uniqueSlug, Content = "Content", Status = ArticleStatus.Draft };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/pages", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var id = created!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/pages/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
