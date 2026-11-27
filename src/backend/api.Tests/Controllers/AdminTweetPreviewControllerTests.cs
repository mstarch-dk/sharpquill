using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.TweetPreviews.DTOs;
using SharpQuill.Application.TweetPreviews.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminTweetPreviewControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly Mock<ITweetPreviewService> _mockTweetPreview;

    public AdminTweetPreviewControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
        _mockTweetPreview = fixture.Factory.MockTweetPreviewService;
        _mockTweetPreview.Reset();
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
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://x.com/user/status/123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FetchPreview_WithEmptyUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithNonTwitterUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://example.com/status/123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithRelativeUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "/x.com/user/status/123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithValidTwitterUrl_ReturnsPreviewData()
    {
        var expectedDto = new TweetPreviewDto("John Doe", "@johndoe", "Hello world!", "March 1, 2024", "https://unavatar.io/twitter/johndoe");
        _mockTweetPreview
            .Setup(s => s.FetchPreviewAsync("https://x.com/johndoe/status/123456"))
            .ReturnsAsync(expectedDto);

        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://x.com/johndoe/status/123456" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TweetPreviewDto>>();
        result.Should().NotBeNull();
        result!.Data.AuthorName.Should().Be("John Doe");
        result.Data.AuthorHandle.Should().Be("@johndoe");
        result.Data.TweetText.Should().Be("Hello world!");
        result.Data.TweetDate.Should().Be("March 1, 2024");
        result.Data.ProfileUrl.Should().Be("https://unavatar.io/twitter/johndoe");
    }

    [Fact]
    public async Task FetchPreview_WithTwitterDotComUrl_ReturnsPreviewData()
    {
        var expectedDto = new TweetPreviewDto("Jane", "@jane", "Test tweet", null, null);
        _mockTweetPreview
            .Setup(s => s.FetchPreviewAsync("https://twitter.com/jane/status/789"))
            .ReturnsAsync(expectedDto);

        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://twitter.com/jane/status/789" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TweetPreviewDto>>();
        result.Should().NotBeNull();
        result!.Data.AuthorName.Should().Be("Jane");
        result.Data.TweetText.Should().Be("Test tweet");
    }

    [Fact]
    public async Task FetchPreview_WhenServiceThrowsHttpRequestException_ReturnsFetchError()
    {
        _mockTweetPreview
            .Setup(s => s.FetchPreviewAsync("https://x.com/deleted/status/999"))
            .ThrowsAsync(new HttpRequestException("Not found"));

        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://x.com/deleted/status/999" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("FETCH_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WhenServiceThrowsTaskCanceledException_ReturnsTimeoutError()
    {
        _mockTweetPreview
            .Setup(s => s.FetchPreviewAsync("https://x.com/slow/status/111"))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://x.com/slow/status/111" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ParseErrorAsync(response);
        error.GetProperty("code").GetString().Should().Be("TIMEOUT_ERROR");
    }

    [Fact]
    public async Task FetchPreview_WithNullableFields_ReturnsPartialData()
    {
        var partialDto = new TweetPreviewDto("Author Only", null, null, null, null);
        _mockTweetPreview
            .Setup(s => s.FetchPreviewAsync("https://x.com/author/status/555"))
            .ReturnsAsync(partialDto);

        var response = await _client.PostAsJsonAsync("/api/admin/tweet-preview", new { Url = "https://x.com/author/status/555" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TweetPreviewDto>>();
        result.Should().NotBeNull();
        result!.Data.AuthorName.Should().Be("Author Only");
        result.Data.AuthorHandle.Should().BeNull();
        result.Data.TweetText.Should().BeNull();
        result.Data.TweetDate.Should().BeNull();
        result.Data.ProfileUrl.Should().BeNull();
    }
}
