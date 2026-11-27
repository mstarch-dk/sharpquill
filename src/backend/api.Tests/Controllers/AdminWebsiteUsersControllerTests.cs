using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.WebsiteUsers.DTOs;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminWebsiteUsersControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminWebsiteUsersControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        var response = await _client.GetAsync("/api/admin/website-users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WebsiteUserDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Data.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetUsers_WithStatusFilter_ReturnsFilteredUsers()
    {
        var response = await _client.GetAsync("/api/admin/website-users?status=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WebsiteUserDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(u => u.Email == TestSeedData.PendingWebsiteUserEmail);
        result.Data.Should().NotContain(u => u.Email == TestSeedData.ApprovedWebsiteUserEmail);
    }

    [Fact]
    public async Task GetCounts_ReturnsStatusCounts()
    {
        var response = await _client.GetAsync("/api/admin/website-users/counts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WebsiteUserCountsDto>>();
        result.Should().NotBeNull();
        result!.Data.Pending.Should().BeGreaterThanOrEqualTo(1);
        result.Data.Approved.Should().BeGreaterThanOrEqualTo(1);
        result.Data.Rejected.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Moderate_ApproveUser_ReturnsNoContent()
    {
        var request = new { Status = 1 }; // Approved

        try
        {
            var response = await _client.PutAsJsonAsync(
                $"/api/admin/website-users/{TestSeedData.PendingWebsiteUserId}/moderate", request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            // Other tests in this class rely on the seeded user being pending; xUnit's test order is not fixed.
            await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
            await using var cmd = dataSource.CreateCommand("UPDATE website_users SET status = 0 WHERE id = @Id");
            cmd.Parameters.AddWithValue("@Id", TestSeedData.PendingWebsiteUserId);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Moderate_NonExistentUser_ReturnsNotFound()
    {
        var request = new { Status = 1 };

        var response = await _client.PutAsJsonAsync("/api/admin/website-users/9999/moderate", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GenerateLoginToken_ForApprovedUser_ReturnsToken()
    {
        var response = await _client.PostAsync(
            $"/api/admin/website-users/{TestSeedData.ApprovedWebsiteUserId}/login-token", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WebsiteLoginTokenDto>>();
        result.Should().NotBeNull();
        result!.Data.Token.Should().NotBeNullOrEmpty();
        result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateLoginToken_ForPendingUser_ReturnsBadRequest()
    {
        var response = await _client.PostAsync(
            $"/api/admin/website-users/{TestSeedData.PendingWebsiteUserId}/login-token", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_STATUS");
    }

    [Fact]
    public async Task GenerateLoginToken_ForNonExistentUser_ReturnsNotFound()
    {
        var response = await _client.PostAsync("/api/admin/website-users/9999/login-token", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/website-users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
