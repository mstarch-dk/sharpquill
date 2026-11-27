using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Likes.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

/// <summary>
/// Likes are anonymous: the browser sends a random visitor id (UUID), and POST toggles that visitor's like.
/// Counts are asserted relative to the start of each test because the article_likes table is shared.
/// </summary>
[Collection("Database")]
public class LikesControllerTests
{
    private const int ArticleId = TestSeedData.PublishedArticle1Id;

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public LikesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient().WithUniqueClientIp();
    }

    private static string LikesUrl(int articleId) => $"/api/v1/articles/{articleId}/likes";

    private static string NewVisitorId() => Guid.NewGuid().ToString();

    private async Task<LikeStatusDto> GetStatus(int articleId, string? visitorId = null)
    {
        var url = visitorId == null ? LikesUrl(articleId) : $"{LikesUrl(articleId)}?visitorId={visitorId}";
        var result = await _client.GetFromJsonAsync<ApiResponse<LikeStatusDto>>(url);
        return result!.Data;
    }

    private async Task<HttpResponseMessage> Toggle(int articleId, string visitorId) =>
        await _client.PostAsJsonAsync(LikesUrl(articleId), new { VisitorId = visitorId });

    [Fact]
    public async Task GetStatus_WithoutVisitorId_ReturnsCountAndNotLiked()
    {
        var expected = await TestSql.ScalarAsync<long>(
            "SELECT COUNT(*) FROM article_likes WHERE article_id = @Id", ("Id", ArticleId));

        var status = await GetStatus(ArticleId);

        status.Count.Should().Be((int)expected);
        status.HasLiked.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_LikesThenUnlikes_ForTheSameVisitor()
    {
        var visitor = NewVisitorId();
        var before = (await GetStatus(ArticleId)).Count;

        var likeResponse = await Toggle(ArticleId, visitor);
        likeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var liked = (await likeResponse.Content.ReadFromJsonAsync<ApiResponse<LikeStatusDto>>())!.Data;
        liked.HasLiked.Should().BeTrue();
        liked.Count.Should().Be(before + 1);
        (await GetStatus(ArticleId, visitor)).HasLiked.Should().BeTrue();

        var unlikeResponse = await Toggle(ArticleId, visitor);
        unlikeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unliked = (await unlikeResponse.Content.ReadFromJsonAsync<ApiResponse<LikeStatusDto>>())!.Data;
        unliked.HasLiked.Should().BeFalse();
        unliked.Count.Should().Be(before);
        (await GetStatus(ArticleId, visitor)).HasLiked.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_StoresOneRowPerVisitor_WithClientIp()
    {
        var visitor = NewVisitorId();

        await Toggle(ArticleId, visitor);

        var rows = await TestSql.ScalarAsync<long>(
            "SELECT COUNT(*) FROM article_likes WHERE article_id = @Id AND visitor_id = @Visitor",
            ("Id", ArticleId), ("Visitor", visitor));
        rows.Should().Be(1);
        var ip = await TestSql.ScalarAsync<string>(
            "SELECT ip_address FROM article_likes WHERE article_id = @Id AND visitor_id = @Visitor",
            ("Id", ArticleId), ("Visitor", visitor));
        ip.Should().Be(_client.ClientIp());
    }

    [Fact]
    public async Task Toggle_DifferentVisitors_AreCountedSeparately()
    {
        var first = NewVisitorId();
        var second = NewVisitorId();
        var before = (await GetStatus(ArticleId)).Count;

        await Toggle(ArticleId, first);
        await Toggle(ArticleId, second);

        (await GetStatus(ArticleId)).Count.Should().Be(before + 2);
        (await GetStatus(ArticleId, first)).HasLiked.Should().BeTrue();
        (await GetStatus(ArticleId, second)).HasLiked.Should().BeTrue();
        (await GetStatus(ArticleId, NewVisitorId())).HasLiked.Should().BeFalse();

        // One visitor unliking leaves the other's like in place
        await Toggle(ArticleId, first);
        (await GetStatus(ArticleId)).Count.Should().Be(before + 1);
        (await GetStatus(ArticleId, second)).HasLiked.Should().BeTrue();
    }

    [Fact]
    public async Task Toggle_LikeOnOneArticle_DoesNotCountOnAnother()
    {
        var visitor = NewVisitorId();
        var otherBefore = (await GetStatus(TestSeedData.PublishedArticle2Id)).Count;

        await Toggle(ArticleId, visitor);

        (await GetStatus(TestSeedData.PublishedArticle2Id)).Count.Should().Be(otherBefore);
        (await GetStatus(TestSeedData.PublishedArticle2Id, visitor)).HasLiked.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("ABCDEF00-1234-5678-9ABC-DEF012345678")] // upper case is not what the web app sends
    [InlineData("00000000-0000-0000-0000-0000000000000")]
    public async Task Toggle_WithInvalidVisitorId_ReturnsBadRequest(string visitorId)
    {
        var response = await Toggle(ArticleId, visitorId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(TestSeedData.DraftArticleId)]
    public async Task Toggle_OnUnknownOrUnpublishedArticle_ReturnsNotFound(int articleId)
    {
        var visitor = NewVisitorId();

        var response = await Toggle(articleId, visitor);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var rows = await TestSql.ScalarAsync<long>(
            "SELECT COUNT(*) FROM article_likes WHERE visitor_id = @Visitor", ("Visitor", visitor));
        rows.Should().Be(0);
    }

    [Fact]
    public async Task Toggle_MoreThanThirtyPerMinuteFromOneIp_IsRateLimited()
    {
        var visitor = NewVisitorId();
        for (var i = 0; i < 30; i++)
        {
            (await Toggle(ArticleId, visitor)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await Toggle(ArticleId, visitor)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Reading the status is not rate limited
        (await _client.GetAsync(LikesUrl(ArticleId))).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
