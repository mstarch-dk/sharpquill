using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Categories.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class CategoriesControllerTests
{
    private readonly HttpClient _client;

    public CategoriesControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(c => c.Slug == TestSeedData.TechnologySlug);
    }

    [Fact]
    public async Task GetArticlesByCategory_WithValidSlug_ReturnsArticles()
    {
        var response = await _client.GetAsync($"/api/v1/categories/{TestSeedData.TechnologySlug}/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetArticlesByCategory_WithInvalidSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/categories/nonexistent-category/articles");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticlesByCategory_WithPagination_RespectsPageSize()
    {
        var response = await _client.GetAsync($"/api/v1/categories/{TestSeedData.TechnologySlug}/articles?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Count.Should().BeLessThanOrEqualTo(1);
    }
}
