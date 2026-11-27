using System.Net;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

[Collection("Database")]
public class JwtSecretValidationTests
{
    private const string StrongSecret = "k7Qp2vX9mR4tW8zL1nB6cF3hJ5gD0sYe";

    private readonly DatabaseFixture _fixture;

    public JwtSecretValidationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too-short-secret")]
    [InlineData("YourSuperSecretKeyThatIsAtLeast32CharactersLong!")]
    [InlineData("CHANGE_ME_32_CHARS_MIN_USE_RANDOM_STRING")]
    public void ValidateSecret_WeakOrPlaceholderSecret_ReportsProblem(string secret)
    {
        new JwtSettings { Secret = secret }.ValidateSecret().Should().NotBeNull();
    }

    [Fact]
    public void ValidateSecret_StrongSecret_IsAccepted()
    {
        new JwtSettings { Secret = StrongSecret }.ValidateSecret().Should().BeNull();
    }

    [Fact]
    public void Startup_InProductionWithPlaceholderSecret_Fails()
    {
        // appsettings.json ships the placeholder secret
        using var factory = _fixture.Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*placeholder*");
    }

    [Fact]
    public void Startup_InProductionWithShortSecret_Fails()
    {
        using var factory = _fixture.Factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Secret", "short"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*shorter than 32*");
    }

    [Fact]
    public async Task Startup_InProductionWithStrongSecret_Succeeds()
    {
        using var factory = _fixture.Factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Secret", StrongSecret));

        var response = await factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
