using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Articles.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class ArticlesControllerTests
{
    private readonly HttpClient _client;

    public ArticlesControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetArticles_ReturnsPublishedArticles()
    {
        var response = await _client.GetAsync("/api/v1/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        // Should only return published articles
        result.Data.Should().Contain(a => a.Slug == TestSeedData.PublishedArticle1Slug);
        result.Data.Should().NotContain(a => a.Slug == TestSeedData.DraftArticleSlug);
    }

    [Fact]
    public async Task GetArticle_BySlug_ReturnsArticle()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.PublishedArticle1Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.PublishedArticle1Slug);
        result.Data.Title.Should().Be("Intro to C#");
        result.Data.Author.Should().NotBeNull();
    }

    [Fact]
    public async Task GetArticle_BySlug_ReturnsTagsWithNamesAndSlugs()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.PublishedArticle1Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result!.Data.Tags.Select(t => (t.Id, t.Name, t.Slug)).Should().BeEquivalentTo(new[]
        {
            (1, "C#", "csharp"),
            (2, ".NET", "dotnet"),
            (3, "Tutorial", "tutorial"),
        });
    }

    [Fact]
    public async Task GetArticle_DraftSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/articles/{TestSeedData.DraftArticleSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticles_Featured_ReturnsFeaturedArticles()
    {
        var response = await _client.GetAsync("/api/v1/articles/featured");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetArticles_WithPagination_RespectsPageSize()
    {
        var response = await _client.GetAsync("/api/v1/articles?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Count.Should().BeLessThanOrEqualTo(1);
    }
}
