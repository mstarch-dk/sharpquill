using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Comments.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

/// <summary>
/// Public comments: anonymous readers post with a name and email, comments wait for moderation, and only approved
/// comments are listed. Members-only (gated) articles need a reader session for both.
/// </summary>
[Collection("Database")]
public class CommentsControllerTests
{
    // Listing tests clear and reseed this article's comments; posting tests use another one.
    private const int ListArticleId = TestSeedData.PublishedArticle2Id;
    private const int PostArticleId = TestSeedData.PublishedArticle3Id;

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public CommentsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient().WithUniqueClientIp();
    }

    private static string CommentsUrl(int articleId) => $"/api/v1/articles/{articleId}/comments";

    private static object ValidComment(string content = "Great article!", int? parentCommentId = null) => new
    {
        AuthorName = "Reader",
        AuthorEmail = "reader@example.com",
        Content = content,
        ParentCommentId = parentCommentId,
        Honeypot = (string?)null
    };

    private static Task<long> CountCommentsWithContent(string content) =>
        TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM comments WHERE content = @Content", ("Content", content));

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return body!.RootElement.GetProperty("error").GetProperty("code").GetString();
    }

    // --- Listing ---

    [Fact]
    public async Task GetComments_ReturnsOnlyApprovedComments_WithRepliesNested()
    {
        await TestSql.ExecuteAsync("DELETE FROM comments WHERE article_id = @Id", ("Id", ListArticleId));
        var older = DateTime.UtcNow.AddHours(-2);
        var parentId = await TestSql.InsertCommentAsync(ListArticleId, "Approved top-level", 1, createdAt: older);
        await TestSql.InsertCommentAsync(ListArticleId, "Approved reply", 1, parentId);
        await TestSql.InsertCommentAsync(ListArticleId, "Pending reply", 0, parentId);
        await TestSql.InsertCommentAsync(ListArticleId, "Second approved", 1);
        await TestSql.InsertCommentAsync(ListArticleId, "Pending top-level", 0);
        await TestSql.InsertCommentAsync(ListArticleId, "Rejected top-level", 2);

        var response = await _client.GetAsync(CommentsUrl(ListArticleId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CommentDto>>>();
        result!.Data.Select(c => c.Content).Should().Equal("Approved top-level", "Second approved");

        var parent = result.Data[0];
        parent.Id.Should().Be(parentId);
        parent.AuthorName.Should().Be("Seeded Reader");
        parent.Replies.Should().ContainSingle();
        parent.Replies[0].Content.Should().Be("Approved reply");
        parent.Replies[0].ParentCommentId.Should().Be(parentId);
        result.Data[1].Replies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComments_DoesNotExposeEmailOrIpAddress()
    {
        await TestSql.ExecuteAsync("DELETE FROM comments WHERE article_id = @Id", ("Id", ListArticleId));
        await TestSql.InsertCommentAsync(ListArticleId, "Visible comment", 1);

        var json = await _client.GetStringAsync(CommentsUrl(ListArticleId));

        json.Should().Contain("Visible comment");
        json.Should().NotContain("seeded@example.com");
        json.Should().NotContain("192.0.2.1");
    }

    [Fact]
    public async Task GetComments_ForArticleWithoutComments_ReturnsEmptyList()
    {
        var response = await _client.GetAsync(CommentsUrl(TestSeedData.PublishedArticle1Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CommentDto>>>();
        result!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComments_OnGatedArticle_WithoutReaderSession_ReturnsSignInRequired()
    {
        var response = await _client.GetAsync(CommentsUrl(TestSeedData.GatedArticleId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCode(response)).Should().Be("SIGN_IN_REQUIRED");
    }

    [Fact]
    public async Task GetComments_OnGatedArticle_WithAuthorToken_ReturnsSignInRequired()
    {
        // An author token is not a reader session
        AuthHelper.CreateAuthenticatedClient(_client);

        var response = await _client.GetAsync(CommentsUrl(TestSeedData.GatedArticleId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetComments_OnGatedArticle_WithReaderSession_ReturnsApprovedComments()
    {
        await TestSql.ExecuteAsync("DELETE FROM comments WHERE article_id = @Id", ("Id", TestSeedData.GatedArticleId));
        await TestSql.InsertCommentAsync(TestSeedData.GatedArticleId, "Members-only comment", 1);
        AuthHelper.CreateWebsiteUserAuthenticatedClient(_client);

        var response = await _client.GetAsync(CommentsUrl(TestSeedData.GatedArticleId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CommentDto>>>();
        result!.Data.Should().ContainSingle(c => c.Content == "Members-only comment");
    }

    // --- Posting ---

    [Fact]
    public async Task CreateComment_Anonymous_CreatesPendingComment()
    {
        var content = $"Pending comment {Guid.NewGuid()}";

        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment(content));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();
        result!.Data.Id.Should().BePositive();
        result.Data.AuthorName.Should().Be("Reader");
        result.Data.Content.Should().Be(content);

        var (status, ip, email) = await ReadStoredComment(result.Data.Id);
        status.Should().Be(0, "new comments wait for moderation");
        ip.Should().Be(_client.ClientIp());
        email.Should().Be("reader@example.com");

        // Not listed until approved
        var list = await _client.GetFromJsonAsync<ApiResponse<List<CommentDto>>>(CommentsUrl(PostArticleId));
        list!.Data.Should().NotContain(c => c.Id == result.Data.Id);
    }

    private static async Task<(int Status, string? Ip, string Email)> ReadStoredComment(int id)
    {
        var status = await TestSql.ScalarAsync<int>("SELECT status FROM comments WHERE id = @Id", ("Id", id));
        var ip = await TestSql.ScalarAsync<string>("SELECT ip_address FROM comments WHERE id = @Id", ("Id", id));
        var email = await TestSql.ScalarAsync<string>("SELECT author_email FROM comments WHERE id = @Id", ("Id", id));
        return (status, ip, email);
    }

    [Fact]
    public async Task CreateComment_ReplyToTopLevelComment_IsStoredWithParent()
    {
        var parentId = await TestSql.InsertCommentAsync(PostArticleId, "Parent for reply", 1);

        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("A reply", parentId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();
        result!.Data.ParentCommentId.Should().Be(parentId);
    }

    [Fact]
    public async Task CreateComment_ReplyToReply_ReturnsBadRequest()
    {
        var parentId = await TestSql.InsertCommentAsync(PostArticleId, "Top", 1);
        var replyId = await TestSql.InsertCommentAsync(PostArticleId, "Reply", 1, parentId);

        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("Nested reply", replyId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCode(response)).Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task CreateComment_ReplyToCommentOnOtherArticle_ReturnsBadRequest()
    {
        var otherArticleComment = await TestSql.InsertCommentAsync(ListArticleId, "Elsewhere", 1);

        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("Cross reply", otherArticleComment));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateComment_ReplyToUnknownComment_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("Orphan", 999999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(TestSeedData.DraftArticleId)]
    public async Task CreateComment_OnUnknownOrUnpublishedArticle_ReturnsNotFound(int articleId)
    {
        var content = $"Should not be stored {Guid.NewGuid()}";

        var response = await _client.PostAsJsonAsync(CommentsUrl(articleId), ValidComment(content));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCode(response)).Should().Be("NOT_FOUND");
        (await CountCommentsWithContent(content)).Should().Be(0);
    }

    [Fact]
    public async Task CreateComment_WhenCommentsDisabled_ReturnsBadRequest()
    {
        var content = $"Disabled {Guid.NewGuid()}";
        await TestSql.ExecuteAsync("UPDATE articles SET comments_enabled = false WHERE id = @Id", ("Id", PostArticleId));
        try
        {
            var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment(content));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await CountCommentsWithContent(content)).Should().Be(0);
        }
        finally
        {
            await TestSql.ExecuteAsync("UPDATE articles SET comments_enabled = true WHERE id = @Id", ("Id", PostArticleId));
        }
    }

    [Fact]
    public async Task CreateComment_WithHoneypot_IsRejectedWithoutStoring()
    {
        var content = $"Spam {Guid.NewGuid()}";

        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), new
        {
            AuthorName = "Bot",
            AuthorEmail = "bot@example.com",
            Content = content,
            ParentCommentId = (int?)null,
            Honeypot = "http://spam.example"
        });

        // The validator rejects a filled honeypot before the service's silent fake success is reached
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CountCommentsWithContent(content)).Should().Be(0);
    }

    public static TheoryData<string, string, string> InvalidComments => new()
    {
        { "", "reader@example.com", "Content" },
        { new string('n', 101), "reader@example.com", "Content" },
        { "Reader", "", "Content" },
        { "Reader", "not-an-email", "Content" },
        { "Reader", "reader@example.com", "" },
        { "Reader", "reader@example.com", new string('c', 5001) },
    };

    [Theory]
    [MemberData(nameof(InvalidComments))]
    public async Task CreateComment_WithInvalidInput_ReturnsBadRequest(string name, string email, string content)
    {
        var response = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), new
        {
            AuthorName = name,
            AuthorEmail = email,
            Content = content,
            ParentCommentId = (int?)null,
            Honeypot = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateComment_OnGatedArticle_WithoutReaderSession_ReturnsSignInRequired()
    {
        var content = $"Gated anonymous {Guid.NewGuid()}";

        var response = await _client.PostAsJsonAsync(CommentsUrl(TestSeedData.GatedArticleId), ValidComment(content));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCode(response)).Should().Be("SIGN_IN_REQUIRED");
        (await CountCommentsWithContent(content)).Should().Be(0);
    }

    [Fact]
    public async Task CreateComment_OnGatedArticle_WithReaderSession_CreatesComment()
    {
        AuthHelper.CreateWebsiteUserAuthenticatedClient(_client);

        var response = await _client.PostAsJsonAsync(CommentsUrl(TestSeedData.GatedArticleId), ValidComment("Member comment"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateComment_MoreThanFivePerMinuteFromOneIp_IsRateLimited()
    {
        for (var i = 0; i < 5; i++)
        {
            var ok = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment($"Burst {i}"));
            ok.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var limited = await _client.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("One too many"));
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Another client IP has its own budget
        var other = _fixture.Factory.CreateClient().WithUniqueClientIp();
        var otherResponse = await other.PostAsJsonAsync(CommentsUrl(PostArticleId), ValidComment("Different reader"));
        otherResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
