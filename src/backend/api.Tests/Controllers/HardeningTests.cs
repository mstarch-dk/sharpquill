using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Settings.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>Small hardening items: image pixel cap, LIKE escaping, exception format, health.</summary>
[Collection("Database")]
public class HardeningTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _admin;

    public HardeningTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _admin = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient());
    }

    // ---- image decompression bomb ----

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return ~crc;
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        static byte[] BigEndian(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];
        var typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(BigEndian((uint)data.Length));
        s.Write(typeAndData);
        s.Write(BigEndian(Crc32(typeAndData)));
    }

    /// <summary>A PNG whose header declares huge dimensions but which is only a few dozen bytes long.</summary>
    private static byte[] CreatePngDeclaring(uint width, uint height)
    {
        static byte[] BigEndian(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = BigEndian(width).Concat(BigEndian(height)).Concat(new byte[] { 8, 2, 0, 0, 0 }).ToArray();
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "IDAT", [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    [Fact]
    public async Task Upload_ImageWithHugeDeclaredDimensions_IsRejected()
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(CreatePngDeclaring(30000, 30000));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "bomb.png");

        var response = await _admin.PostAsync("/api/admin/media", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("too large");
    }

    // ---- LIKE wildcards ----

    [Fact]
    public async Task TitleAutocomplete_TreatsPercentAndUnderscoreLiterally()
    {
        var percent = await _admin.GetFromJsonAsync<ApiResponse<List<TitleResult>>>("/api/admin/title-autocomplete?q=%25");
        var underscore = await _admin.GetFromJsonAsync<ApiResponse<List<TitleResult>>>("/api/admin/title-autocomplete?q=_");

        percent!.Data.Should().BeEmpty("no seeded title contains a percent sign");
        underscore!.Data.Should().BeEmpty("no seeded title contains an underscore");
    }

    private record TitleResult(int Id, string Title, string Slug, string ContentType, string Url);

    // ---- exception format ----

    [Fact]
    public async Task UnhandledException_ReturnsErrorFormat_WithoutDetails()
    {
        var settings = new Mock<ISiteSettingsService>();
        settings.Setup(s => s.GetAllAsync()).ThrowsAsync(new InvalidOperationException("secret-internal-detail"));
        using var factory = _fixture.Factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services => services.AddScoped(_ => settings.Object)));
        var client = AuthHelper.CreateAuthenticatedClient(factory.CreateClient());

        var response = await client.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INTERNAL_ERROR");
        body.Should().NotContain("secret-internal-detail");
    }

    // ---- health ----

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        var response = await _fixture.Factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("healthy");
    }
}
