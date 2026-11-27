using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.LinkPreviews.DTOs;
using SharpQuill.Application.LinkPreviews.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminLinkPreviewControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly Mock<ILinkPreviewService> _mockLinkPreview;

    public AdminLinkPreviewControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
        _mockLinkPreview = fixture.Factory.MockLinkPreviewService;
        _mockLinkPreview.Reset();
    }

    private static async Task<JsonElement> ParseErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("error");
    }

    [Fact]
    public async Task FetchPreview_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/admin/link-preview", new { Url = "https://example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FetchPreview_WithEmptyUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithWhitespaceUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithRelativeUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "/relative/path" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithNonHttpUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "ftp://example.com/file" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithValidUrl_ReturnsPreviewData()
    {
        var expectedDto = new LinkPreviewDto("Example Site", "A description", "https://example.com/image.png", "Example");
        _mockLinkPreview
            .Setup(s => s.FetchPreviewAsync("https://example.com"))
            .ReturnsAsync(expectedDto);

        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "https://example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LinkPreviewDto>>();
        result.Should().NotBeNull();
        result!.Data.Title.Should().Be("Example Site");
        result.Data.Description.Should().Be("A description");
        result.Data.ImageUrl.Should().Be("https://example.com/image.png");
        result.Data.SiteName.Should().Be("Example");
    }

    [Fact]
    public async Task FetchPreview_WhenServiceThrowsHttpRequestException_ReturnsFetchError()
    {
        _mockLinkPreview
            .Setup(s => s.FetchPreviewAsync("https://unreachable.example.com"))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "https://unreachable.example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("FETCH_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WhenServiceThrowsTaskCanceledException_ReturnsTimeoutError()
    {
        _mockLinkPreview
            .Setup(s => s.FetchPreviewAsync("https://slow.example.com"))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "https://slow.example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("TIMEOUT_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithNullableFields_ReturnsPartialData()
    {
        var partialDto = new LinkPreviewDto("Title Only", null, null, null);
        _mockLinkPreview
            .Setup(s => s.FetchPreviewAsync("https://minimal.example.com"))
            .ReturnsAsync(partialDto);

        var response = await _client.PostAsJsonAsync("/api/admin/link-preview", new { Url = "https://minimal.example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LinkPreviewDto>>();
        result.Should().NotBeNull();
        result!.Data.Title.Should().Be("Title Only");
        result.Data.Description.Should().BeNull();
        result.Data.ImageUrl.Should().BeNull();
        result.Data.SiteName.Should().BeNull();
    }
}
