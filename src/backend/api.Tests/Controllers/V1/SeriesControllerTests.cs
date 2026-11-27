using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Series.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class SeriesControllerTests
{
    private readonly HttpClient _client;

    public SeriesControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetSeries_ReturnsAllSeries()
    {
        var response = await _client.GetAsync("/api/v1/series");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SeriesDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(s => s.Slug == TestSeedData.GettingStartedSeriesSlug);
    }

    [Fact]
    public async Task GetSeriesBySlug_WithValidSlug_ReturnsSeriesWithArticles()
    {
        var response = await _client.GetAsync($"/api/v1/series/{TestSeedData.GettingStartedSeriesSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeriesDetailDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.GettingStartedSeriesSlug);
        result.Data.Articles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSeriesBySlug_ArticlesAreOrdered()
    {
        var response = await _client.GetAsync($"/api/v1/series/{TestSeedData.GettingStartedSeriesSlug}");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeriesDetailDto>>();
        result!.Data.Articles.Should().BeInAscendingOrder(a => a.Order);
    }

    [Fact]
    public async Task GetSeriesBySlug_WithInvalidSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/series/nonexistent-series");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
