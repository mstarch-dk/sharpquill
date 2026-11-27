using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Settings.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminSettingsControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminSettingsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetSettings_ReturnsSettings()
    {
        var response = await _client.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SiteSettingsDto>>();
        result.Should().NotBeNull();
        result!.Data.SiteName.Should().Be("Test CMS");
    }

    [Fact]
    public async Task GetSettings_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettings_WithValidData_ReturnsUpdated()
    {
        var dto = new
        {
            SiteName = "Updated CMS",
            SiteDescription = "Updated description",
            SiteUrl = "https://updated.example.com",
            ContactEmail = "updated@example.com",
            GlobalFont = "manrope"
        };

        var response = await _client.PutAsJsonAsync("/api/admin/settings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SiteSettingsDto>>();
        result!.Data.SiteName.Should().Be("Updated CMS");
        result.Data.GlobalFont.Should().Be("manrope");

        // Restore original values
        var restoreDto = new
        {
            SiteName = "Test CMS",
            SiteDescription = "A test CMS site",
            SiteUrl = "https://test.example.com",
            ContactEmail = "test@example.com",
            GlobalFont = "inter"
        };
        await _client.PutAsJsonAsync("/api/admin/settings", restoreDto);
    }

    [Fact]
    public async Task UpdateSettings_WithInvalidGlobalFont_ReturnsValidationError()
    {
        var dto = new
        {
            GlobalFont = "definitely-not-supported"
        };

        var response = await _client.PutAsJsonAsync("/api/admin/settings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("GlobalFont");
        body.Should().Contain("inter, manrope, source-serif-4, atkinson-hyperlegible");
    }

    [Fact]
    public async Task UpdateSettings_PersistsLanguageAndFooterText_AndPublicEndpointExposesThem()
    {
        var dto = new
        {
            SiteName = "Brand Co",
            SiteDescription = "We write things",
            Language = "en-GB",
            FooterText = "(c) {year} Brand Co. All rights reserved."
        };

        var response = await _client.PutAsJsonAsync("/api/admin/settings", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ApiResponse<SiteSettingsDto>>();
        updated!.Data.Language.Should().Be("en-GB");
        updated.Data.FooterText.Should().Be("(c) {year} Brand Co. All rights reserved.");

        var publicResponse = await _fixture.Factory.CreateClient().GetAsync("/api/v1/settings");
        var publicSettings = await publicResponse.Content.ReadFromJsonAsync<ApiResponse<PublicSiteSettingsDto>>();
        publicSettings!.Data.SiteName.Should().Be("Brand Co");
        publicSettings.Data.Language.Should().Be("en-GB");
        publicSettings.Data.FooterText.Should().Be("(c) {year} Brand Co. All rights reserved.");

        // Restore original values
        await _client.PutAsJsonAsync("/api/admin/settings", new
        {
            SiteName = "Test CMS",
            SiteDescription = "A test CMS site",
            SiteUrl = "https://test.example.com",
            ContactEmail = "test@example.com",
            GlobalFont = "inter"
        });
    }

    [Theory]
    [InlineData("da")]
    [InlineData("en")]
    [InlineData("en-GB")]
    [InlineData("zh-Hans-CN")]
    public async Task UpdateSettings_WithValidLanguage_Succeeds(string language)
    {
        var response = await _client.PutAsJsonAsync("/api/admin/settings", new { Language = language });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _client.PutAsJsonAsync("/api/admin/settings", new { SiteName = "Test CMS", SiteDescription = "A test CMS site" });
    }

    [Theory]
    [InlineData("english")]
    [InlineData("da\" onload=\"x")]
    [InlineData("d")]
    [InlineData("en_GB")]
    public async Task UpdateSettings_WithInvalidLanguage_ReturnsValidationError(string language)
    {
        var response = await _client.PutAsJsonAsync("/api/admin/settings", new { Language = language });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Language");
    }

    [Fact]
    public async Task UpdateSettings_WithOverlongFooterText_ReturnsValidationError()
    {
        var response = await _client.PutAsJsonAsync("/api/admin/settings", new { FooterText = new string('x', 501) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FooterText");
    }

    [Fact]
    public async Task UpdateSettings_PersistsFooterContactDetails_AndPublicEndpointExposesThem()
    {
        var response = await _client.PutAsJsonAsync("/api/admin/settings", new
        {
            SiteName = "Test CMS",
            SiteDescription = "A test CMS site",
            PublicEmail = "hello@example.com",
            PublicPhone = "+45 12 34 56 78",
            CompanyInfo = "CVR 12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await _fixture.Factory.CreateClient().GetAsync("/api/v1/settings");
        var publicSettings = await publicResponse.Content.ReadFromJsonAsync<ApiResponse<PublicSiteSettingsDto>>();
        publicSettings!.Data.PublicEmail.Should().Be("hello@example.com");
        publicSettings.Data.PublicPhone.Should().Be("+45 12 34 56 78");
        publicSettings.Data.CompanyInfo.Should().Be("CVR 12345678");

        await _client.PutAsJsonAsync("/api/admin/settings", new { SiteName = "Test CMS", SiteDescription = "A test CMS site" });
    }

    [Theory]
    [InlineData("not-an-email", null, "PublicEmail")]
    [InlineData(null, "javascript:alert(1)", "PublicPhone")]
    public async Task UpdateSettings_WithInvalidFooterContactDetails_ReturnsValidationError(
        string? publicEmail, string? publicPhone, string field)
    {
        var response = await _client.PutAsJsonAsync("/api/admin/settings", new { PublicEmail = publicEmail, PublicPhone = publicPhone });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(field);
    }
}
