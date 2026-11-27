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

namespace SharpQuill.Api.Tests.Controllers.V1;

[Collection("Database")]
public class WebsiteAuthControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public WebsiteAuthControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Signup_WithValidData_ReturnsCreated()
    {
        var request = new { Name = "New User", Email = "newuser@example.com" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WebsiteUserDto>>();
        result.Should().NotBeNull();
        result!.Data.Name.Should().Be("New User");
        result.Data.Email.Should().Be("newuser@example.com");
        result.Data.Status.Should().Be(Core.WebsiteUsers.WebsiteUserStatus.Pending);
    }

    [Fact]
    public async Task Signup_WithDuplicateEmail_IsIndistinguishableFromNewSignup()
    {
        var existing = await _client.PostAsJsonAsync("/api/v1/auth/signup",
            new { Name = "Duplicate", Email = TestSeedData.ApprovedWebsiteUserEmail });
        var fresh = await _client.PostAsJsonAsync("/api/v1/auth/signup",
            new { Name = "Duplicate", Email = $"fresh-{Guid.NewGuid():N}@example.com" });

        existing.StatusCode.Should().Be(HttpStatusCode.Created);
        fresh.StatusCode.Should().Be(HttpStatusCode.Created);

        var existingBody = await existing.Content.ReadFromJsonAsync<ApiResponse<WebsiteUserDto>>();
        existingBody!.Data.Status.Should().Be(Core.WebsiteUsers.WebsiteUserStatus.Pending);
        existingBody.Data.Id.Should().Be(0, "the existing account must not be revealed");
    }

    [Theory]
    [InlineData("", "a@example.com")]
    [InlineData("Name", "not-an-email")]
    [InlineData("Name", "")]
    public async Task Signup_WithInvalidData_ReturnsBadRequest(string name, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", new { Name = name, Email = email });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidToken_ReturnsAccessToken()
    {
        var request = new { Token = TestSeedData.ValidLoginToken };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WebsiteLoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.ExpiresIn.Should().BeGreaterThan(0);
        result.Data.User.Should().NotBeNull();
        result.Data.User.Email.Should().Be(TestSeedData.ApprovedWebsiteUserEmail);
    }

    [Fact]
    public async Task Login_WithUsedToken_ReturnsUnauthorized()
    {
        var request = new { Token = TestSeedData.UsedLoginToken };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_TOKEN");
    }

    [Fact]
    public async Task Login_WithExpiredToken_ReturnsUnauthorized()
    {
        var request = new { Token = TestSeedData.ExpiredLoginToken };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_TOKEN");
    }

    [Fact]
    public async Task Login_WithNonExistentToken_ReturnsUnauthorized()
    {
        var request = new { Token = "nonexistent_token" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signup_WhenLoginDisabled_ReturnsForbiddenAndCreatesNoUser()
    {
        var email = $"disabled-{Guid.NewGuid():N}@example.com";

        var response = await WithLoginEnabledSetting("false",
            () => _client.PostAsJsonAsync("/api/v1/auth/signup", new { Name = "Blocked", Email = email }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("LOGIN_DISABLED");
        (await CountWebsiteUsersWithEmail(email)).Should().Be(0);
    }

    [Fact]
    public async Task Signup_WhenLoginExplicitlyEnabled_ReturnsCreated()
    {
        var email = $"enabled-{Guid.NewGuid():N}@example.com";

        var response = await WithLoginEnabledSetting("true",
            () => _client.PostAsJsonAsync("/api/v1/auth/signup", new { Name = "Allowed", Email = email }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await CountWebsiteUsersWithEmail(email)).Should().Be(1);
    }

    [Fact]
    public async Task Login_WhenLoginDisabled_ReturnsForbiddenWithoutConsumingToken()
    {
        var adminClient = _fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(adminClient);
        var tokenResponse = await adminClient.PostAsync(
            $"/api/admin/website-users/{TestSeedData.ApprovedWebsiteUserId}/login-token", null);
        var token = (await tokenResponse.Content.ReadFromJsonAsync<ApiResponse<WebsiteLoginTokenDto>>())!.Data.Token;

        var disabledResponse = await WithLoginEnabledSetting("false",
            () => _client.PostAsJsonAsync("/api/v1/auth/login", new { Token = token }));

        disabledResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        disabledResponse.Headers.Contains("Set-Cookie").Should().BeFalse();
        var body = await disabledResponse.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("LOGIN_DISABLED");

        var enabledResponse = await WithLoginEnabledSetting("true",
            () => _client.PostAsJsonAsync("/api/v1/auth/login", new { Token = token }));
        enabledResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Runs the action with the login_enabled site setting set to the given value, then restores the previous value.
    /// </summary>
    private static async Task<T> WithLoginEnabledSetting<T>(string value, Func<Task<T>> action)
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var readCmd = dataSource.CreateCommand("SELECT value FROM site_settings WHERE key = 'login_enabled'");
        var previous = await readCmd.ExecuteScalarAsync();

        await SetLoginEnabled(dataSource, value);
        try
        {
            return await action();
        }
        finally
        {
            if (previous is string previousValue)
            {
                await SetLoginEnabled(dataSource, previousValue);
            }
            else
            {
                await using var deleteCmd = dataSource.CreateCommand("DELETE FROM site_settings WHERE key = 'login_enabled'");
                await deleteCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task SetLoginEnabled(NpgsqlDataSource dataSource, string value)
    {
        await using var cmd = dataSource.CreateCommand(@"
            INSERT INTO site_settings (key, value, updated_at) VALUES ('login_enabled', @Value, NOW())
            ON CONFLICT (key) DO UPDATE SET value = @Value, updated_at = NOW()");
        cmd.Parameters.AddWithValue("@Value", value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountWebsiteUsersWithEmail(string email)
    {
        await using var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);
        await using var cmd = dataSource.CreateCommand("SELECT COUNT(*) FROM website_users WHERE email = @Email");
        cmd.Parameters.AddWithValue("@Email", email);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task GetProfile_WithWebsiteUserToken_ReturnsProfile()
    {
        var client = _fixture.Factory.CreateClient();
        AuthHelper.CreateWebsiteUserAuthenticatedClient(client);

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<WebsiteUserProfileDto>>();
        result.Should().NotBeNull();
        result!.Data.Id.Should().Be(TestSeedData.ApprovedWebsiteUserId);
        result.Data.Email.Should().Be(TestSeedData.ApprovedWebsiteUserEmail);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithAdminToken_ReturnsUnauthorized()
    {
        // Admin tokens should not work for website auth endpoints
        var client = _fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(client);

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
