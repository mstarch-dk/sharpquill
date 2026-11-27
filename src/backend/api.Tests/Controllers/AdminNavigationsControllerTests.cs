using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Navigations;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminNavigationsControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminNavigationsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetNavigations_ReturnsAllNavigations()
    {
        var response = await _client.GetAsync("/api/admin/navigations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<NavigationSummaryDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(n => n.Slug == TestSeedData.MainMenuSlug);
    }

    [Fact]
    public async Task GetNavigations_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/navigations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNavigationById_WithValidId_ReturnsNavigation()
    {
        var response = await _client.GetAsync($"/api/admin/navigations/{TestSeedData.MainMenuId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NavigationAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Slug.Should().Be(TestSeedData.MainMenuSlug);
        result.Data.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNavigationById_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/navigations/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveItems_WithValidData_ReturnsOk()
    {
        // Get current items first
        var getResponse = await _client.GetAsync($"/api/admin/navigations/{TestSeedData.FooterLinksId}");
        var nav = await getResponse.Content.ReadFromJsonAsync<ApiResponse<NavigationAdminDto>>();

        // Save with updated items
        var dto = new
        {
            Items = new[]
            {
                new { Label = "Hjem", ItemType = NavigationItemType.SystemRoute, SystemRoute = "/", Position = 1, OpenInNewTab = false },
                new { Label = "Blog", ItemType = NavigationItemType.SystemRoute, SystemRoute = "/blog", Position = 2, OpenInNewTab = false }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/admin/navigations/{TestSeedData.FooterLinksId}/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
