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
public class AdminArticleVersionsControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminArticleVersionsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetVersions_ReturnsVersionsList()
    {
        var response = await _client.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleVersionDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data[0].VersionNumber.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetVersions_WithInvalidArticleId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/articles/99999/versions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersion_ReturnsVersionDetail()
    {
        // First get the versions list to find a valid version ID
        var listResponse = await _client.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions");
        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleVersionDto>>>();
        var versionId = listResult!.Data[0].Id;

        var response = await _client.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions/{versionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleVersionDetailDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.Content.Should().NotBeNullOrEmpty();
        result.Data.VersionNumber.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetVersion_WithInvalidVersionId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateArticle_CreatesVersion1()
    {
        var uniqueSlug = $"version-test-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Version Test Article",
            Slug = uniqueSlug,
            Content = "Initial content",
            Excerpt = "Initial excerpt",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Verify version 1 was created
        created.Data.VersionNumber.Should().Be(1);
        created.Data.TotalVersions.Should().Be(1);

        // Verify version is listed
        var versionsResponse = await _client.GetAsync($"/api/admin/articles/{articleId}/versions");
        var versions = await versionsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleVersionDto>>>();
        versions!.Data.Should().HaveCount(1);
        versions.Data[0].VersionNumber.Should().Be(1);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task UpdateArticle_WithContentChange_CreatesNewVersion()
    {
        var uniqueSlug = $"version-update-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Version Update Test",
            Slug = uniqueSlug,
            Content = "Original content",
            Excerpt = "Original excerpt",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Update with different content
        var updateDto = new
        {
            Title = "Version Update Test",
            Slug = uniqueSlug,
            Content = "Updated content",
            Excerpt = "Updated excerpt",
            Status = ArticleStatus.Draft
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/articles/{articleId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();

        updated!.Data.VersionNumber.Should().Be(2);
        updated.Data.TotalVersions.Should().Be(2);
        updated.Data.Content.Should().Be("Updated content");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task UpdateArticle_WithSameContent_DoesNotCreateNewVersion()
    {
        var uniqueSlug = $"version-same-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "No Version Change Test",
            Slug = uniqueSlug,
            Content = "Same content",
            Excerpt = "Same excerpt",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Update with only metadata change (title), same content
        var updateDto = new
        {
            Title = "Updated Title Only",
            Slug = uniqueSlug,
            Content = "Same content",
            Excerpt = "Same excerpt",
            Status = ArticleStatus.Draft
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/articles/{articleId}", updateDto);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();

        // Should still be version 1, only 1 total version
        updated!.Data.TotalVersions.Should().Be(1);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task PublishVersion_SetsPublishedVersion()
    {
        var uniqueSlug = $"version-publish-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Publish Version Test",
            Slug = uniqueSlug,
            Content = "V1 content",
            Status = ArticleStatus.Published
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;
        var v1PublishedVersionId = created.Data.PublishedVersionId;
        v1PublishedVersionId.Should().NotBeNull();

        // Update with different content to create version 2
        var updateDto = new
        {
            Title = "Publish Version Test",
            Slug = uniqueSlug,
            Content = "V2 content",
            Status = ArticleStatus.Published
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/articles/{articleId}", updateDto);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var v2PublishedVersionId = updated!.Data.PublishedVersionId;
        v2PublishedVersionId.Should().NotBe(v1PublishedVersionId);

        // Now publish version 1 again
        var publishResponse = await _client.PutAsJsonAsync(
            $"/api/admin/articles/{articleId}/versions/{v1PublishedVersionId}/publish", new { });
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        published!.Data.PublishedVersionId.Should().Be(v1PublishedVersionId);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task PublishVersion_WithInvalidVersionId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions/99999/publish", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersions_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync($"/api/admin/articles/{TestSeedData.PublishedArticle1Id}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteArticle_CascadeDeletesVersions()
    {
        var uniqueSlug = $"version-cascade-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Cascade Delete Test",
            Slug = uniqueSlug,
            Content = "Content",
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Delete article
        var deleteResponse = await _client.DeleteAsync($"/api/admin/articles/{articleId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify article and versions are gone
        var getResponse = await _client.GetAsync($"/api/admin/articles/{articleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
