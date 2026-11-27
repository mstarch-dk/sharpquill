using System.Net;
using SharpQuill.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class CorsAndHeadersTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string AdminOrigin = "https://admin.test";
    private const string WebOrigin = "https://web.test";

    private readonly HttpClient _client;

    public CorsAndHeadersTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Cors:AdminOrigins:0", AdminOrigin);
            b.UseSetting("Cors:PublicOrigins:0", WebOrigin);
        });
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // xUnit creates this class (and so a host) per test; dispose it so its background services and connections go.
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private async Task<HttpResponseMessage> PreflightAsync(string path, string origin, string method = "POST")
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        return await _client.SendAsync(request);
    }

    private static string? AllowedOrigin(HttpResponseMessage r) =>
        r.Headers.TryGetValues("Access-Control-Allow-Origin", out var v) ? v.Single() : null;

    private static string? AllowCredentials(HttpResponseMessage r) =>
        r.Headers.TryGetValues("Access-Control-Allow-Credentials", out var v) ? v.Single() : null;

    [Fact]
    public async Task AdminEndpoint_AllowsAdminOrigin_WithCredentials()
    {
        var response = await PreflightAsync("/api/admin/auth/refresh", AdminOrigin);

        AllowedOrigin(response).Should().Be(AdminOrigin);
        AllowCredentials(response).Should().Be("true");
    }

    [Fact]
    public async Task AdminEndpoint_RejectsPublicWebOrigin()
    {
        var response = await PreflightAsync("/api/admin/auth/refresh", WebOrigin);

        AllowedOrigin(response).Should().BeNull();
    }

    [Fact]
    public async Task PublicEndpoint_AllowsWebOrigin_WithCredentials()
    {
        var response = await PreflightAsync("/api/v1/auth/refresh", WebOrigin);

        AllowedOrigin(response).Should().Be(WebOrigin);
        AllowCredentials(response).Should().Be("true");
    }

    [Fact]
    public async Task PublicEndpoint_AdminOrigin_MayOnlyReadWithoutCredentials()
    {
        var read = await PreflightAsync("/api/v1/settings", AdminOrigin, "GET");
        var write = await PreflightAsync("/api/v1/auth/refresh", AdminOrigin, "POST");

        AllowedOrigin(read).Should().Be(AdminOrigin);
        AllowCredentials(read).Should().BeNull();
        // The preflight lists only the permitted methods, so a browser will not send the POST
        write.Headers.GetValues("Access-Control-Allow-Methods").Single().Should().NotContain("POST");
        AllowCredentials(write).Should().BeNull();
    }

    [Fact]
    public async Task Responses_CarryBaselineSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle("strict-origin-when-cross-origin");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("SAMEORIGIN");
    }
}
