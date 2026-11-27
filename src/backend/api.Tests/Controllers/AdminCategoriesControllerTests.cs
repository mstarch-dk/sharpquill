using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Categories.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminCategoriesControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminCategoriesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        var response = await _client.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(c => c.Slug == TestSeedData.TechnologySlug);
    }

    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategory_WithValidId_ReturnsCategory()
    {
        var response = await _client.GetAsync($"/api/admin/categories/{TestSeedData.TechnologyCategoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.TechnologySlug);
    }

    [Fact]
    public async Task GetCategory_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/categories/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        var uniqueSlug = $"test-cat-{Guid.NewGuid():N}";
        var dto = new { Name = "Test Category", Slug = uniqueSlug, Description = "A test category" };

        var response = await _client.PostAsJsonAsync("/api/admin/categories", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result.Should().NotBeNull();
        result!.Data.Name.Should().Be("Test Category");
        result.Data.Slug.Should().Be(uniqueSlug);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/categories/{result.Data.Id}");
    }

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsUpdated()
    {
        // Create
        var uniqueSlug = $"test-cat-upd-{Guid.NewGuid():N}";
        var createDto = new { Name = "Original", Slug = uniqueSlug, Description = "Desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var id = created!.Data.Id;

        // Update
        var updateDto = new { Name = "Updated", Slug = uniqueSlug, Description = "Updated desc" };
        var response = await _client.PutAsJsonAsync($"/api/admin/categories/{id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        result!.Data.Name.Should().Be("Updated");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/categories/{id}");
    }

    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsNoContent()
    {
        var uniqueSlug = $"test-cat-del-{Guid.NewGuid():N}";
        var dto = new { Name = "To Delete", Slug = uniqueSlug };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/categories", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var id = created!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/categories/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
