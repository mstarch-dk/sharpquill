using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Auth.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AuthControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AuthControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var loginRequest = new { Email = "admin@example.com", Password = "admin123" };

        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.Author.Should().NotBeNull();
        result.Data.Author.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        var loginRequest = new { Email = "nonexistent@example.com", Password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
    {
        var loginRequest = new { Email = "invalid-email", Password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsProfile()
    {
        AuthHelper.CreateAuthenticatedClient(_client);

        var response = await _client.GetAsync("/api/admin/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthorProfileDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        var response = await _client.PostAsync("/api/admin/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CertLogin_WithValidThumbprint_ReturnsToken()
    {
        AuthHelper.SetCertificateHeader(_client, TestSeedData.CertThumbprint);

        var response = await _client.PostAsync("/api/admin/auth/cert-login", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.Author.Email.Should().Be(TestSeedData.CertAuthorEmail);
    }

    [Fact]
    public async Task CertLogin_WithInvalidThumbprint_ReturnsUnauthorized()
    {
        AuthHelper.SetCertificateHeader(_client, "INVALID_THUMBPRINT");

        var response = await _client.PostAsync("/api/admin/auth/cert-login", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_CERTIFICATE");
    }

    [Fact]
    public async Task CertLogin_WithNoThumbprint_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsync("/api/admin/auth/cert-login", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NO_CERTIFICATE");
    }

    [Fact]
    public async Task Refresh_ForUserRoleAuthor_PreservesUserRole()
    {
        var thumbprint = $"USER_ROLE_{Guid.NewGuid():N}";
        await using (var dataSource = NpgsqlDataSource.Create(DatabaseFixture.ConnectionString))
        await using (var cmd = dataSource.CreateCommand(@"
            INSERT INTO authors (email, password_hash, name, role, certificate_thumbprint, created_at)
            VALUES (@Email, 'not-a-real-hash', 'Regular User', 'user', @Thumbprint, NOW())"))
        {
            cmd.Parameters.AddWithValue("@Email", $"{thumbprint.ToLowerInvariant()}@example.com");
            cmd.Parameters.AddWithValue("@Thumbprint", thumbprint);
            await cmd.ExecuteNonQueryAsync();
        }

        var client = _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        AuthHelper.SetCertificateHeader(client, thumbprint);
        var loginResponse = await client.PostAsync("/api/admin/auth/cert-login", null);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("refreshToken="))
            .Split(';')[0];

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/admin/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refreshResponse = await client.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponseDto>>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result!.Data.AccessToken);
        jwt.Claims.Single(c => c.Type == "role").Value.Should().Be("user");

        var adminClient = _fixture.Factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Data.AccessToken);
        var authorsResponse = await adminClient.GetAsync("/api/admin/authors");
        authorsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_ExceedingRateLimit_ReturnsTooManyRequestsPerForwardedClientIp()
    {
        using var factory = _fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:AuthPermitLimit", "2"));
        var client = factory.CreateClient();
        var loginRequest = new { Email = "nonexistent@example.com", Password = "password123" };

        async Task<HttpStatusCode> LoginFrom(string clientIp)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/auth/login")
            {
                Content = JsonContent.Create(loginRequest)
            };
            request.Headers.Add("X-Forwarded-For", clientIp);
            return (await client.SendAsync(request)).StatusCode;
        }

        (await LoginFrom("203.0.113.1")).Should().Be(HttpStatusCode.Unauthorized);
        (await LoginFrom("203.0.113.1")).Should().Be(HttpStatusCode.Unauthorized);
        (await LoginFrom("203.0.113.1")).Should().Be(HttpStatusCode.TooManyRequests);
        (await LoginFrom("203.0.113.2")).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithNoToken_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsync("/api/admin/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("INVALID_TOKEN");
    }
}
