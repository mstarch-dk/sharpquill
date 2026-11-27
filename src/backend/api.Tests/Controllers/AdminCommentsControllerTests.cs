using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Core.Comments;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// Comment moderation. Every author may moderate (AdminUser policy), readers and anonymous callers may not.
/// The comments table is shared by the whole test collection, so assertions are relative to the rows a test inserts.
/// </summary>
[Collection("Database")]
public class AdminCommentsControllerTests
{
    private const string BaseUrl = "/api/admin/comments";
    private const int ArticleId = TestSeedData.PublishedArticle1Id;

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _admin;

    public AdminCommentsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _admin = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient());
    }

    private static Task<int> StoredStatus(int id) =>
        TestSql.ScalarAsync<int>("SELECT status FROM comments WHERE id = @Id", ("Id", id));

    private static Task<long> CountWithStatus(int status) =>
        TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM comments WHERE status = @Status", ("Status", status));

    [Fact]
    public async Task GetComments_ReturnsNewestFirst_WithArticleTitleAndPrivateFields()
    {
        var olderId = await TestSql.InsertCommentAsync(ArticleId, "Admin list older", 0, createdAt: DateTime.UtcNow.AddSeconds(-1));
        var newerId = await TestSql.InsertCommentAsync(ArticleId, "Admin list newer", 2);

        var response = await _admin.GetAsync($"{BaseUrl}?pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CommentAdminDto>>>();
        var ids = result!.Data.Select(c => c.Id).ToList();
        ids.Should().ContainInOrder(newerId, olderId);

        var newer = result.Data.Single(c => c.Id == newerId);
        newer.ArticleId.Should().Be(ArticleId);
        newer.ArticleTitle.Should().Be("Intro to C#");
        newer.AuthorEmail.Should().Be("seeded@example.com");
        newer.IpAddress.Should().Be("192.0.2.1");
        newer.Status.Should().Be(CommentStatus.Rejected);
    }

    [Fact]
    public async Task GetComments_WithStatusFilter_ReturnsOnlyThatStatus()
    {
        var pendingId = await TestSql.InsertCommentAsync(ArticleId, "Filter pending", 0);
        var approvedId = await TestSql.InsertCommentAsync(ArticleId, "Filter approved", 1);

        var response = await _admin.GetAsync($"{BaseUrl}?status=0&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data").EnumerateArray().ToList();
        data.Should().OnlyContain(c => c.GetProperty("status").GetInt32() == 0);
        data.Select(c => c.GetProperty("id").GetInt32()).Should().Contain(pendingId).And.NotContain(approvedId);

        var meta = body.RootElement.GetProperty("meta");
        meta.GetProperty("totalCount").GetInt32().Should().Be((int)await CountWithStatus(0));
    }

    [Fact]
    public async Task GetComments_Pagination_ReturnsRequestedPageAndMeta()
    {
        await TestSql.InsertCommentAsync(ArticleId, "Page a", 1);
        await TestSql.InsertCommentAsync(ArticleId, "Page b", 1);
        await TestSql.InsertCommentAsync(ArticleId, "Page c", 1);
        var total = await TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM comments");

        var page1 = await _admin.GetFromJsonAsync<JsonDocument>($"{BaseUrl}?page=1&pageSize=2");
        var page2 = await _admin.GetFromJsonAsync<JsonDocument>($"{BaseUrl}?page=2&pageSize=2");

        page1!.RootElement.GetProperty("data").GetArrayLength().Should().Be(2);
        var meta = page1.RootElement.GetProperty("meta");
        meta.GetProperty("page").GetInt32().Should().Be(1);
        meta.GetProperty("pageSize").GetInt32().Should().Be(2);
        meta.GetProperty("totalCount").GetInt32().Should().Be((int)total);
        meta.GetProperty("totalPages").GetInt32().Should().Be((int)Math.Ceiling(total / 2.0));

        var firstIds = page1.RootElement.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("id").GetInt32());
        var secondIds = page2!.RootElement.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("id").GetInt32());
        secondIds.Should().NotIntersectWith(firstIds);
    }

    [Fact]
    public async Task GetCounts_MatchesStoredStatuses()
    {
        await TestSql.InsertCommentAsync(ArticleId, "Count pending", 0);
        await TestSql.InsertCommentAsync(ArticleId, "Count approved", 1);
        await TestSql.InsertCommentAsync(ArticleId, "Count rejected", 2);

        var response = await _admin.GetAsync($"{BaseUrl}/counts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentCountsDto>>();
        result!.Data.Pending.Should().Be((int)await CountWithStatus(0)).And.BePositive();
        result.Data.Approved.Should().Be((int)await CountWithStatus(1)).And.BePositive();
        result.Data.Rejected.Should().Be((int)await CountWithStatus(2)).And.BePositive();
    }

    [Fact]
    public async Task GetComment_ById_ReturnsComment()
    {
        var parentId = await TestSql.InsertCommentAsync(ArticleId, "Get parent", 1);
        var id = await TestSql.InsertCommentAsync(ArticleId, "Get by id", 0, parentId);

        var response = await _admin.GetAsync($"{BaseUrl}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentAdminDto>>();
        result!.Data.Id.Should().Be(id);
        result.Data.Content.Should().Be("Get by id");
        result.Data.ParentCommentId.Should().Be(parentId);
        result.Data.Status.Should().Be(CommentStatus.Pending);
        result.Data.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetComment_Unknown_ReturnsNotFound()
    {
        var response = await _admin.GetAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(CommentStatus.Approved)]
    [InlineData(CommentStatus.Rejected)]
    public async Task Moderate_SetsStatusAndUpdatedAt(CommentStatus status)
    {
        var id = await TestSql.InsertCommentAsync(ArticleId, $"Moderate to {status}", 0);

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{id}/moderate", new { Status = status });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredStatus(id)).Should().Be((int)status);
        var comment = await _admin.GetFromJsonAsync<ApiResponse<CommentAdminDto>>($"{BaseUrl}/{id}");
        comment!.Data.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Moderate_ApprovedComment_AppearsInPublicListing()
    {
        var id = await TestSql.InsertCommentAsync(ArticleId, "Approve me publicly", 0);
        var publicClient = _fixture.Factory.CreateClient();

        var before = await publicClient.GetFromJsonAsync<ApiResponse<List<CommentDto>>>($"/api/v1/articles/{ArticleId}/comments");
        before!.Data.Should().NotContain(c => c.Id == id);

        await _admin.PutAsJsonAsync($"{BaseUrl}/{id}/moderate", new { Status = CommentStatus.Approved });

        var after = await publicClient.GetFromJsonAsync<ApiResponse<List<CommentDto>>>($"/api/v1/articles/{ArticleId}/comments");
        after!.Data.Should().Contain(c => c.Id == id);
    }

    [Fact]
    public async Task Moderate_BackToPending_ReturnsBadRequest()
    {
        var id = await TestSql.InsertCommentAsync(ArticleId, "Stay approved", 1);

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{id}/moderate", new { Status = CommentStatus.Pending });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredStatus(id)).Should().Be(1);
    }

    [Fact]
    public async Task Moderate_Unknown_ReturnsNotFound()
    {
        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/999999/moderate", new { Status = CommentStatus.Approved });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesComment()
    {
        var id = await TestSql.InsertCommentAsync(ArticleId, "Delete me", 0);

        var response = await _admin.DeleteAsync($"{BaseUrl}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM comments WHERE id = @Id", ("Id", id))).Should().Be(0);
        (await _admin.GetAsync($"{BaseUrl}/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Unknown_ReturnsNotFound()
    {
        var response = await _admin.DeleteAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UserRoleAuthor_CanModerate()
    {
        // Comment moderation is open to every author, not only the admin role
        var user = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), TestSeedData.CertAuthorId, "user");
        var id = await TestSql.InsertCommentAsync(ArticleId, "Moderated by user role", 0);

        (await user.GetAsync(BaseUrl)).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await user.PutAsJsonAsync($"{BaseUrl}/{id}/moderate", new { Status = CommentStatus.Approved });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredStatus(id)).Should().Be(1);
    }

    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", BaseUrl },
        { "GET", $"{BaseUrl}/counts" },
        { "GET", $"{BaseUrl}/{{id}}" },
        { "PUT", $"{BaseUrl}/{{id}}/moderate" },
        { "DELETE", $"{BaseUrl}/{{id}}" },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoints_WithoutToken_ReturnUnauthorized(string method, string url)
    {
        var id = await TestSql.InsertCommentAsync(ArticleId, "Anonymous target", 0);

        var response = await Send(_fixture.Factory.CreateClient(), method, url.Replace("{id}", id.ToString()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredStatus(id)).Should().Be(0, "the comment must be untouched");
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoints_WithReaderToken_AreRejected(string method, string url)
    {
        var reader = AuthHelper.CreateWebsiteUserAuthenticatedClient(_fixture.Factory.CreateClient());
        var id = await TestSql.InsertCommentAsync(ArticleId, "Reader target", 0);

        var response = await Send(reader, method, url.Replace("{id}", id.ToString()));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        (await StoredStatus(id)).Should().Be(0, "the comment must be untouched");
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string url) => method switch
    {
        "GET" => client.GetAsync(url),
        "PUT" => client.PutAsJsonAsync(url, new { Status = CommentStatus.Approved }),
        "DELETE" => client.DeleteAsync(url),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}
