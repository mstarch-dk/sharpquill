using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Tags.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminTagsControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminTagsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetTags_ReturnsAllTags()
    {
        var response = await _client.GetAsync("/api/admin/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TagDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(t => t.Slug == TestSeedData.CSharpTagSlug);
    }

    [Fact]
    public async Task GetTags_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/tags");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTag_WithValidId_ReturnsTag()
    {
        var response = await _client.GetAsync($"/api/admin/tags/{TestSeedData.CSharpTagId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TagDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.CSharpTagSlug);
    }

    [Fact]
    public async Task GetTag_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/tags/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTag_WithValidData_ReturnsCreated()
    {
        var uniqueSlug = $"test-tag-{Guid.NewGuid():N}";
        var dto = new { Name = "Test Tag", Slug = uniqueSlug };

        var response = await _client.PostAsJsonAsync("/api/admin/tags", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TagDto>>();
        result.Should().NotBeNull();
        result!.Data.Name.Should().Be("Test Tag");
        result.Data.Slug.Should().Be(uniqueSlug);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/tags/{result.Data.Id}");
    }

    [Fact]
    public async Task UpdateTag_WithValidData_ReturnsUpdated()
    {
        var uniqueSlug = $"test-tag-upd-{Guid.NewGuid():N}";
        var createDto = new { Name = "Original Tag", Slug = uniqueSlug };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/tags", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TagDto>>();
        var id = created!.Data.Id;

        var updateDto = new { Name = "Updated Tag", Slug = uniqueSlug };
        var response = await _client.PutAsJsonAsync($"/api/admin/tags/{id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TagDto>>();
        result!.Data.Name.Should().Be("Updated Tag");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/tags/{id}");
    }

    [Fact]
    public async Task DeleteTag_WithValidId_ReturnsNoContent()
    {
        var uniqueSlug = $"test-tag-del-{Guid.NewGuid():N}";
        var dto = new { Name = "To Delete", Slug = uniqueSlug };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/tags", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TagDto>>();
        var id = created!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/tags/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
