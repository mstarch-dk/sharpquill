using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// Refresh and magic-link tokens: stored hashed, rotated atomically, replay-detected and revoked on account changes.
/// </summary>
[Collection("Database")]
public class TokenLifecycleTests
{
    private readonly DatabaseFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public TokenLifecycleTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        // The shared factory already lifts the auth and refresh rate limits (TestWebApplicationFactory).
        _factory = fixture.Factory;
    }

    private static NpgsqlDataSource Db() => NpgsqlDataSource.Create(DatabaseFixture.ConnectionString);

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    // ---- helpers ----------------------------------------------------------------------------------

    private static async Task<int> CreateAuthorAsync(string role)
    {
        var thumbprint = $"TL_{Guid.NewGuid():N}";
        await using var db = Db();
        await using var cmd = db.CreateCommand(@"
            INSERT INTO authors (email, password_hash, name, role, certificate_thumbprint, created_at)
            VALUES (@Email, 'not-a-real-hash', 'Token Test', @Role, @Thumb, NOW()) RETURNING id");
        cmd.Parameters.AddWithValue("@Email", $"{thumbprint.ToLowerInvariant()}@example.com");
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@Thumb", thumbprint);
        var id = (int)(await cmd.ExecuteScalarAsync())!;
        _thumbprints[id] = thumbprint;
        return id;
    }

    private static readonly Dictionary<int, string> _thumbprints = new();

    /// <summary>Signs in through cert-login and returns the raw refresh token.</summary>
    private async Task<string> AdminLoginAsync(int authorId)
    {
        var client = NewClient();
        AuthHelper.SetCertificateHeader(client, _thumbprints[authorId]);
        var response = await client.PostAsync("/api/admin/auth/cert-login", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return CookieValue(response, "refreshToken");
    }

    private static string CookieValue(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith(name + "="))
            .Split(';')[0][(name.Length + 1)..]
            .Pipe(Uri.UnescapeDataString);

    private async Task<HttpResponseMessage> RefreshAsync(string path, string cookieName, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{cookieName}={Uri.EscapeDataString(token)}");
        return await NewClient().SendAsync(request);
    }

    private Task<HttpResponseMessage> AdminRefreshAsync(string token) =>
        RefreshAsync("/api/admin/auth/refresh", "refreshToken", token);

    private Task<HttpResponseMessage> WebsiteRefreshAsync(string token) =>
        RefreshAsync("/api/v1/auth/refresh", "websiteRefreshToken", token);

    private static async Task<T> ScalarAsync<T>(string sql, params (string, object)[] ps)
    {
        await using var db = Db();
        await using var cmd = db.CreateCommand(sql);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task ExecAsync(string sql, params (string, object)[] ps)
    {
        await using var db = Db();
        await using var cmd = db.CreateCommand(sql);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CreateWebsiteUserAsync(int status = 1)
    {
        var email = $"tl_{Guid.NewGuid():N}@example.com";
        return await ScalarAsync<int>(@"
            INSERT INTO website_users (name, email, status, created_at)
            VALUES ('TL User', @Email, @Status, NOW()) RETURNING id",
            ("@Email", email), ("@Status", status));
    }

    private static async Task<string> CreateLoginTokenAsync(int websiteUserId)
    {
        var raw = $"tl_login_{Guid.NewGuid():N}";
        await ExecAsync(@"
            INSERT INTO website_login_tokens (website_user_id, token, expires_at, created_at)
            VALUES (@Uid, @Hash, NOW() + interval '1 day', NOW())",
            ("@Uid", websiteUserId), ("@Hash", TokenHasher.Hash(raw)));
        return raw;
    }

    private async Task<string> WebsiteLoginAsync(string loginToken)
    {
        var response = await NewClient().PostAsJsonAsync("/api/v1/auth/login", new { Token = loginToken });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return CookieValue(response, "websiteRefreshToken");
    }

    // ---- admin refresh tokens ---------------------------------------------------------------------

    [Fact]
    public async Task RefreshToken_IsStoredOnlyAsHash()
    {
        var authorId = await CreateAuthorAsync("user");

        var raw = await AdminLoginAsync(authorId);

        var plaintext = await ScalarAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE token = @T", ("@T", raw));
        var hashed = await ScalarAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE token = @T AND author_id = @A",
            ("@T", TokenHasher.Hash(raw)), ("@A", authorId));
        plaintext.Should().Be(0);
        hashed.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldTokenIsRejected()
    {
        var authorId = await CreateAuthorAsync("user");
        var first = await AdminLoginAsync(authorId);

        var refreshed = await AdminRefreshAsync(first);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = CookieValue(refreshed, "refreshToken");

        second.Should().NotBe(first);
        (await AdminRefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Immediate replay is treated as a concurrent refresh, not theft: the new token survives
        (await AdminRefreshAsync(second)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReplayOfRotatedTokenAfterGracePeriod_RevokesAllSessions()
    {
        var authorId = await CreateAuthorAsync("user");
        var stolen = await AdminLoginAsync(authorId);
        var current = CookieValue(await AdminRefreshAsync(stolen), "refreshToken");

        // Pretend the rotation happened a while ago
        await ExecAsync("UPDATE refresh_tokens SET revoked_at = NOW() - interval '5 minutes' WHERE token = @T",
            ("@T", TokenHasher.Hash(stolen)));

        (await AdminRefreshAsync(stolen)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await AdminRefreshAsync(current)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ConcurrentRequestsWithSameToken_SucceedAtMostOnce()
    {
        var authorId = await CreateAuthorAsync("user");
        var token = await AdminLoginAsync(authorId);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AdminRefreshAsync(token)));

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
    }

    [Fact]
    public async Task UpdatingAuthorPasswordOrRole_RevokesRefreshTokens()
    {
        var admin = AuthHelper.CreateAuthenticatedClient(_factory.CreateClient(), 1, "admin");

        // password change
        var byPassword = await CreateAuthorAsync("user");
        var passwordToken = await AdminLoginAsync(byPassword);
        var email = await ScalarAsync<string>("SELECT email FROM authors WHERE id = @I", ("@I", byPassword));
        var update = await admin.PutAsJsonAsync($"/api/admin/authors/{byPassword}", new
        {
            Email = email, Name = "Token Test", Role = "user", Password = "a-new-password-123",
            Bio = (string?)null, AvatarUrl = (string?)null, CertificateThumbprint = _thumbprints[byPassword]
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AdminRefreshAsync(passwordToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // role change
        var byRole = await CreateAuthorAsync("user");
        var roleToken = await AdminLoginAsync(byRole);
        email = await ScalarAsync<string>("SELECT email FROM authors WHERE id = @I", ("@I", byRole));
        update = await admin.PutAsJsonAsync($"/api/admin/authors/{byRole}", new
        {
            Email = email, Name = "Token Test", Role = "admin", Password = (string?)null,
            Bio = (string?)null, AvatarUrl = (string?)null, CertificateThumbprint = _thumbprints[byRole]
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AdminRefreshAsync(roleToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- website tokens ---------------------------------------------------------------------------

    [Fact]
    public async Task LoginToken_IsStoredHashed_AndRedeemableOnlyOnceUnderConcurrency()
    {
        var userId = await CreateWebsiteUserAsync();
        var raw = await CreateLoginTokenAsync(userId);

        (await ScalarAsync<long>("SELECT COUNT(*) FROM website_login_tokens WHERE token = @T", ("@T", raw)))
            .Should().Be(0);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            NewClient().PostAsJsonAsync("/api/v1/auth/login", new { Token = raw })));

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
    }

    [Fact]
    public async Task WebsiteRefreshToken_IsStoredHashed_AndRotates()
    {
        var userId = await CreateWebsiteUserAsync();
        var first = await WebsiteLoginAsync(await CreateLoginTokenAsync(userId));

        (await ScalarAsync<long>("SELECT COUNT(*) FROM website_refresh_tokens WHERE token = @T", ("@T", first)))
            .Should().Be(0);

        var refreshed = await WebsiteRefreshAsync(first);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await WebsiteRefreshAsync(first)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebsiteRefresh_ForUserRejectedDirectlyInDb_IsDenied()
    {
        var userId = await CreateWebsiteUserAsync();
        var token = await WebsiteLoginAsync(await CreateLoginTokenAsync(userId));
        await ExecAsync("UPDATE website_users SET status = 2 WHERE id = @I", ("@I", userId));

        (await WebsiteRefreshAsync(token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectingWebsiteUser_RevokesTheirSessions()
    {
        var userId = await CreateWebsiteUserAsync();
        var token = await WebsiteLoginAsync(await CreateLoginTokenAsync(userId));
        var admin = AuthHelper.CreateAuthenticatedClient(_factory.CreateClient(), 1, "admin");

        var moderate = await admin.PutAsJsonAsync($"/api/admin/website-users/{userId}/moderate", new { Status = 2 });
        moderate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ScalarAsync<long>(
            "SELECT COUNT(*) FROM website_refresh_tokens WHERE website_user_id = @I AND revoked_at IS NULL",
            ("@I", userId))).Should().Be(0);
        (await WebsiteRefreshAsync(token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- cleanup ----------------------------------------------------------------------------------

    [Fact]
    public async Task TokenCleanup_RemovesLongExpiredTokensOnly()
    {
        var authorId = await CreateAuthorAsync("user");
        var expired = $"expired_{Guid.NewGuid():N}";
        var live = $"live_{Guid.NewGuid():N}";
        await ExecAsync(@"
            INSERT INTO refresh_tokens (token, author_id, expires_at, created_at) VALUES
              (@E, @A, NOW() - interval '3 days', NOW() - interval '10 days'),
              (@L, @A, NOW() + interval '3 days', NOW())",
            ("@E", expired), ("@L", live), ("@A", authorId));

        await using var db = Db();
        await new TokenCleanupService(db, NullLogger<TokenCleanupService>.Instance, TimeProvider.System).PurgeAsync();

        (await ScalarAsync<long>("SELECT COUNT(*) FROM refresh_tokens WHERE token = @T", ("@T", expired))).Should().Be(0);
        (await ScalarAsync<long>("SELECT COUNT(*) FROM refresh_tokens WHERE token = @T", ("@T", live))).Should().Be(1);
    }
}

internal static class PipeExtensions
{
    public static TOut Pipe<TIn, TOut>(this TIn value, Func<TIn, TOut> func) => func(value);
}
