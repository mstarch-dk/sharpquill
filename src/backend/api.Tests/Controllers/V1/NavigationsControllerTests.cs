using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Navigations.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class NavigationsControllerTests
{
    private readonly HttpClient _client;

    public NavigationsControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetNavigationBySlug_WithValidSlug_ReturnsNavigation()
    {
        var response = await _client.GetAsync($"/api/v1/navigations/{TestSeedData.MainMenuSlug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NavigationDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.MainMenuSlug);
        result.Data.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNavigationBySlug_WithInvalidSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/navigations/nonexistent-nav");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
