using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Tags.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class TagsControllerTests
{
    private readonly HttpClient _client;

    public TagsControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetTags_ReturnsAllTags()
    {
        var response = await _client.GetAsync("/api/v1/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TagDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(t => t.Slug == TestSeedData.CSharpTagSlug);
    }

    [Fact]
    public async Task GetArticlesByTag_WithValidSlug_ReturnsArticles()
    {
        var response = await _client.GetAsync($"/api/v1/tags/{TestSeedData.CSharpTagSlug}/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetArticlesByTag_WithInvalidSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/tags/nonexistent-tag/articles");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticlesByTag_WithPagination_RespectsPageSize()
    {
        var response = await _client.GetAsync($"/api/v1/tags/{TestSeedData.CSharpTagSlug}/articles?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Count.Should().BeLessThanOrEqualTo(1);
    }
}
