using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Auth.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// The ASP.NET Core authentication/authorization pipeline: token validation (algorithm, issuer, lifetime, claims),
/// scheme separation, role and ownership policies, error envelopes and logging of rejected tokens.
/// </summary>
[Collection("Database")]
public class AuthPipelineTests : IAsyncLifetime
{
    private const string Secret = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    private const string Issuer = "https://localhost:5000";
    private const string AdminAudience = "https://localhost:5000";
    private const string WebsiteAudience = "https://localhost:5000/website";

    private readonly DatabaseFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly CapturingLoggerProvider _logs = new();

    public AuthPipelineTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = fixture.Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            b.UseSetting("RateLimiting:RefreshPermitLimit", "1000");
            b.ConfigureServices(s => s.AddSingleton<ILoggerProvider>(_logs));
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // xUnit creates this class (and so a host) per test; dispose it so its background services and connections go.
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Client(string? token = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        if (token != null) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static List<Claim> AdminClaims(string role = "admin", string? userType = "admin", string sub = "1")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sub),
            new(JwtRegisteredClaimNames.Email, "admin@example.com"),
            new(JwtRegisteredClaimNames.Name, "Admin"),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (userType != null) claims.Add(new Claim("user_type", userType));
        return claims;
    }

    private static string Token(
        IEnumerable<Claim> claims,
        string audience = AdminAudience,
        string issuer = Issuer,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTime? expires = null,
        string secret = Secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var exp = expires ?? DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: exp < DateTime.UtcNow ? exp.AddHours(-1) : null,
            expires: exp,
            signingCredentials: new SigningCredentials(key, algorithm));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Base64Url(string json) =>
        Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));

    private static async Task<ApiError> ErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error");
        return new ApiError(error.GetProperty("code").GetString()!, error.GetProperty("message").GetString()!);
    }

    // ---- token validation -------------------------------------------------------------------------

    [Fact]
    public async Task UnsignedAlgNoneToken_IsRejected()
    {
        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payload = Base64Url($$"""
            {"sub":"1","email":"admin@example.com","name":"Admin","role":"admin","user_type":"admin",
             "iss":"{{Issuer}}","aud":"{{AdminAudience}}","exp":{{exp}}}
            """);

        var response = await Client($"{header}.{payload}.").GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSignedWithOtherHmacAlgorithm_IsRejected()
    {
        // Same key, HS384 instead of the pinned HS256
        var token = Token(AdminClaims(), algorithm: SecurityAlgorithms.HmacSha384);

        var response = await Client(token).GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSignedWithOtherKey_IsRejected()
    {
        var token = Token(AdminClaims(), secret: "AnotherSecretThatIsAlsoAtLeast32CharactersLong!!");

        var response = await Client(token).GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var token = Token(AdminClaims(), expires: DateTime.UtcNow.AddSeconds(-5));

        var response = await Client(token).GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenFromOtherIssuer_IsRejected()
    {
        var token = Token(AdminClaims(), issuer: "https://evil.example.com");

        var response = await Client(token).GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithNonNumericSubject_IsRejected()
    {
        var token = Token(AdminClaims(sub: "not-a-number"));

        var response = await Client(token).GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithoutRoleClaim_IsNotTreatedAsAdmin()
    {
        var claims = AdminClaims().Where(c => c.Type != "role").ToList();
        var client = Client(Token(claims));

        (await client.GetAsync("/api/admin/settings")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/admin/website-users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- scheme separation ------------------------------------------------------------------------

    [Fact]
    public async Task WebsiteTokenWithoutUserType_IsRejectedOnWebsiteEndpoint()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(JwtRegisteredClaimNames.Email, "approved@example.com"),
            new Claim(JwtRegisteredClaimNames.Name, "Approved User")
        };

        var response = await Client(Token(claims, WebsiteAudience)).GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminToken_OnWebsiteOnlyEndpoint_IsRejected()
    {
        var response = await Client(AuthHelper.GenerateTestToken()).GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminClaimsWithWebsiteAudience_IsRejectedOnWebsiteEndpoint()
    {
        // Correct audience for the website scheme, but the identity is an author: user_type must match the scheme
        var response = await Client(Token(AdminClaims(), WebsiteAudience)).GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- error envelopes --------------------------------------------------------------------------

    [Fact]
    public async Task MissingToken_Returns401WithErrorEnvelope()
    {
        var response = await Client().GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle(h => h.Scheme == "Bearer");
        var error = await ErrorAsync(response);
        error.Code.Should().Be("UNAUTHORIZED");
        error.Message.Should().Be("Authentication required");
    }

    [Fact]
    public async Task InvalidToken_OnWebsiteEndpoint_Returns401WithErrorEnvelope()
    {
        var response = await Client("garbage").GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorAsync(response)).Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task UserRole_OnAdminRoleEndpoint_Returns403WithErrorEnvelope()
    {
        var response = await Client(AuthHelper.GenerateTestToken(2, role: "user")).GetAsync("/api/admin/website-users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await ErrorAsync(response);
        error.Code.Should().Be("FORBIDDEN");
        error.Message.Should().Be("Admin role required");
    }

    [Fact]
    public async Task NonOwner_EditingAnotherAuthorsArticle_Returns403WithErrorEnvelope()
    {
        var admin = Client(AuthHelper.GenerateTestToken(1, role: "admin"));
        var user = Client(AuthHelper.GenerateTestToken(2, role: "user"));
        var create = await admin.PostAsJsonAsync("/api/admin/articles",
            new { Title = "Owned by admin", Slug = $"owned-{Guid.NewGuid():N}", Content = "x" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetInt32();

        var update = await user.PutAsJsonAsync($"/api/admin/articles/{id}",
            new { Title = "Hijacked", Slug = $"hijack-{Guid.NewGuid():N}", Content = "x", Status = ArticleStatus.Draft });

        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await ErrorAsync(update);
        error.Code.Should().Be("FORBIDDEN");
        error.Message.Should().Be("You can only modify your own articles");

        await admin.DeleteAsync($"/api/admin/articles/{id}");
    }

    // ---- logging ----------------------------------------------------------------------------------

    [Fact]
    public async Task RejectedToken_IsLoggedWithoutTokenContents()
    {
        var token = Token(AdminClaims(), issuer: "https://evil.example.com");

        await Client(token).GetAsync("/api/admin/settings");

        var rejected = _logs.Messages.Where(m => m.Contains("Rejected JWT")).ToList();
        rejected.Should().NotBeEmpty();
        rejected.Should().AllSatisfy(m =>
        {
            m.Should().NotContain(token);
            m.Should().NotContain(token.Split('.')[1]);
            m.Should().NotContain(token.Split('.')[2]);
        });
    }

    // ---- refresh keeps the real role --------------------------------------------------------------

    [Fact]
    public async Task RefreshedToken_ForUserRoleAuthor_KeepsUserRole()
    {
        var thumbprint = $"AP_{Guid.NewGuid():N}";
        await using (var db = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString))
        await using (var cmd = db.CreateCommand(@"
            INSERT INTO authors (email, password_hash, name, role, certificate_thumbprint, created_at)
            VALUES (@Email, 'not-a-real-hash', 'Pipeline Test', 'user', @Thumb, NOW())"))
        {
            cmd.Parameters.AddWithValue("@Email", $"{thumbprint.ToLowerInvariant()}@example.com");
            cmd.Parameters.AddWithValue("@Thumb", thumbprint);
            await cmd.ExecuteNonQueryAsync();
        }

        var loginClient = Client();
        AuthHelper.SetCertificateHeader(loginClient, thumbprint);
        var login = await loginClient.PostAsync("/api/admin/auth/cert-login", null);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshCookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refreshToken=")).Split(';')[0];

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/admin/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refresh = await Client().SendAsync(refreshRequest);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = (await refresh.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>())!.Data.AccessToken;

        new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims
            .Single(c => c.Type == "role").Value.Should().Be("user");
        var client = Client(accessToken);
        (await client.GetAsync("/api/admin/settings")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/admin/website-users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                provider.Messages.Enqueue(formatter(state, exception) + (exception == null ? "" : " " + exception));
        }
    }
}
