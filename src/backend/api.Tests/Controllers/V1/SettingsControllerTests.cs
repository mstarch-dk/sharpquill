using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Settings.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class SettingsControllerTests
{
    private readonly HttpClient _client;

    public SettingsControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetSettings_ReturnsSiteSettings()
    {
        var response = await _client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PublicSiteSettingsDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.SiteName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSettings_DoesNotExposeTheContactFormRecipient()
    {
        // The test seed has contact_email set (the admin endpoint returns it); the public endpoint must not
        var json = await (await _client.GetAsync("/api/v1/settings")).Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        data.TryGetProperty("contactEmail", out _).Should().BeFalse();
        json.Should().NotContain("test@example.com");
        data.GetProperty("siteName").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
