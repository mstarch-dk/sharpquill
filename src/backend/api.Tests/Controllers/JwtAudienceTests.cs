using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// Admin and website tokens must not be interchangeable, and a token without user_type is never trusted.
/// </summary>
[Collection("Database")]
public class JwtAudienceTests
{
    private const string AdminAudience = "https://localhost:5000";
    private const string WebsiteAudience = "https://localhost:5000/website";

    private readonly DatabaseFixture _fixture;

    public JwtAudienceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static Claim[] Claims(string? userType)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Email, "admin@example.com"),
            new(JwtRegisteredClaimNames.Name, "Admin"),
            new("role", "admin"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (userType != null) claims.Add(new Claim("user_type", userType));
        return claims.ToArray();
    }

    private async Task<HttpStatusCode> CallAdminEndpointAsync(string token)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return (await client.GetAsync("/api/admin/settings")).StatusCode;
    }

    [Fact]
    public async Task AdminToken_WithAdminAudience_IsAccepted()
    {
        var status = await CallAdminEndpointAsync(AuthHelper.GenerateCustomToken(Claims("admin"), AdminAudience));

        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithoutUserType_IsRejected()
    {
        var status = await CallAdminEndpointAsync(AuthHelper.GenerateCustomToken(Claims(null), AdminAudience));

        status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminClaimsWithWebsiteAudience_IsRejected()
    {
        var status = await CallAdminEndpointAsync(AuthHelper.GenerateCustomToken(Claims("admin"), WebsiteAudience));

        status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebsiteToken_OnAdminEndpoint_IsRejected()
    {
        var status = await CallAdminEndpointAsync(AuthHelper.GenerateWebsiteUserTestToken());

        status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebsiteToken_OnProfileEndpoint_PassesAuthentication()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", AuthHelper.GenerateWebsiteUserTestToken());

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebsiteClaimsWithAdminAudience_IsRejected()
    {
        // A website identity presented with the admin audience must not authenticate.
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", AuthHelper.GenerateCustomToken(Claims("website"), AdminAudience));

        var response = await client.GetAsync("/api/v1/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
