using System.Net;
using SharpQuill.Api.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class HealthControllerTests
{
    private readonly HttpClient _client;

    public HealthControllerTests(DatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
