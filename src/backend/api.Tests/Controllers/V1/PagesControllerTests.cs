using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Articles.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class PagesControllerTests
{
    private readonly HttpClient _client;

    public PagesControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetPageBySlug_WithValidSlug_ReturnsPage()
    {
        var response = await _client.GetAsync($"/api/v1/pages/{TestSeedData.HjemPageSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.HjemPageSlug);
        result.Data.Title.Should().Be("Hjem");
    }

    [Fact]
    public async Task GetPageBySlug_WithInvalidSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/pages/nonexistent-page");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
