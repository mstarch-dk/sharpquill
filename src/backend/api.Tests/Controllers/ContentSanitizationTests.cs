using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Navigations;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>Rendered HTML is sanitised and script-capable URLs are refused (audit 2.4, 2.12).</summary>
[Collection("Database")]
public class ContentSanitizationTests
{
    private const string Attack =
        "Hello <script>alert(1)</script><img src=x onerror=alert(2)> [click](javascript:alert(3)) " +
        "<a href=\"JaVaScRiPt:alert(4)\">x</a> <iframe src=\"https://evil.example\"></iframe> **bold**";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _admin;

    public ContentSanitizationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _admin = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient());
    }

    private async Task<string> CreatePublishedLayoutArticleAsync(object[] blocks)
    {
        var slug = $"san-{Guid.NewGuid():N}";
        var response = await _admin.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Sanitisation",
            Slug = slug,
            Status = ArticleStatus.Published,
            Layout = new { Sections = new[] { new { Type = "full", Blocks = blocks } } }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return slug;
    }

    [Fact]
    public async Task RenderedTextBlock_StripsScriptsEventHandlersAndScriptUrls()
    {
        var slug = await CreatePublishedLayoutArticleAsync([new { Type = "text", Content = Attack }]);

        var raw = await _fixture.Factory.CreateClient().GetStringAsync($"/api/v1/articles/{slug}");

        // The "content" field is the author's raw markdown (the frontends sanitise it when they render it);
        // the composed layout HTML is what the server vouches for.
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        var html = doc.RootElement.GetProperty("data").GetProperty("layout").GetProperty("sections")[0]
            .GetProperty("blocks")[0].GetProperty("html").GetString()!;

        html.Should().NotContainEquivalentOf("<script");
        html.Should().NotContainEquivalentOf("onerror");
        html.Should().NotContainEquivalentOf("javascript:");
        html.Should().NotContainEquivalentOf("<iframe");
        html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public async Task RenderedTextBlock_KeepsKnownImageSize_DropsUnknownOrMisplacedOnes()
    {
        const string content =
            "<img src=\"/uploads/a_medium.png\" alt=\"a\" data-size=\"medium\" data-src-original=\"/uploads/a.png\" />\n\n" +
            "<img src=\"/uploads/b.png\" alt=\"b\" data-size=\"huge\" />\n\n" +
            "<p data-size=\"small\">text</p>";
        var slug = await CreatePublishedLayoutArticleAsync([new { Type = "text", Content = content }]);

        var raw = await _fixture.Factory.CreateClient().GetStringAsync($"/api/v1/articles/{slug}");
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        var html = doc.RootElement.GetProperty("data").GetProperty("layout").GetProperty("sections")[0]
            .GetProperty("blocks")[0].GetProperty("html").GetString()!;

        html.Should().Contain("data-size=\"medium\"");
        html.Should().NotContain("huge");
        html.Should().NotContain("<p data-size");
        html.Should().NotContain("data-src-original");
    }

    [Theory]
    [InlineData("link", "javascript:alert(1)")]
    [InlineData("link", "data:text/html,<script>alert(1)</script>")]
    public async Task LayoutBlock_WithScriptUrl_IsRejected(string type, string url)
    {
        var response = await _admin.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Bad link",
            Slug = $"bad-{Guid.NewGuid():N}",
            Status = ArticleStatus.Draft,
            Layout = new { Sections = new[] { new { Type = "full", Blocks = new object[] { new { Type = type, Url = url, Title = "x" } } } } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NavigationItem_WithScriptUrl_IsRejected()
    {
        var response = await _admin.PutAsJsonAsync($"/api/admin/navigations/{TestSeedData.FooterLinksId}/items", new
        {
            Items = new[]
            {
                new { Label = "Evil", ItemType = NavigationItemType.ExternalUrl, Url = "javascript:alert(1)", Position = 1, OpenInNewTab = false }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SiteSettings_WithScriptUrlInSocialLink_IsRejected()
    {
        var response = await _admin.PutAsJsonAsync("/api/admin/settings", new
        {
            SiteName = "Test CMS",
            SocialTwitter = "javascript:alert(1)"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
