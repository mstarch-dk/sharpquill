using System.Net;
using System.Net.Sockets;
using System.Text;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Application.LinkPreviews.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// Exercises the real LinkPreviewService and its connection guard. The API test factory replaces the
/// service with a mock, so these tests build it directly.
/// </summary>
public class LinkPreviewServiceTests
{
    [Theory]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://localhost/")]
    [InlineData("http://10.0.0.1/")]
    [InlineData("http://172.16.5.4/")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://0.0.0.0/")]
    [InlineData("http://[::1]/")]
    [InlineData("http://[::ffff:127.0.0.1]/")]
    [InlineData("http://[fd00::1]/")]
    public async Task FetchPreview_NonPublicAddress_IsRefused(string url)
    {
        var service = new LinkPreviewService(new HttpClient(PublicAddressHttpHandler.Create()));

        var act = () => service.FetchPreviewAsync(url);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/")]
    [InlineData("gopher://127.0.0.1:6379/")]
    [InlineData("not a url")]
    public async Task FetchPreview_NonHttpUrl_IsRefused(string url)
    {
        var service = new LinkPreviewService(new HttpClient(PublicAddressHttpHandler.Create()));

        var act = () => service.FetchPreviewAsync(url);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task FetchPreview_AllowedAddress_ParsesMetaTagsAndFollowsRedirects()
    {
        using var server = new TestHttpServer(path => path == "/redirect"
            ? (302, null, "/page")
            : (200, Html("Hello preview"), null));
        var service = CreateServiceAllowingOnlyLoopbackServer();

        var preview = await service.FetchPreviewAsync($"{server.BaseUrl}/redirect");

        preview.Title.Should().Be("Hello preview");
    }

    [Fact]
    public async Task FetchPreview_RedirectToNonPublicAddress_IsRefused()
    {
        using var server = new TestHttpServer(_ => (302, null, "http://169.254.169.254/latest/meta-data/"));
        var service = CreateServiceAllowingOnlyLoopbackServer();

        var act = () => service.FetchPreviewAsync($"{server.BaseUrl}/");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task FetchPreview_ContentBeyondSizeCap_IsNotRead()
    {
        var padding = new string(' ', LinkPreviewService.MaxResponseBytes + 1024);
        using var server = new TestHttpServer(path => path == "/head-first"
            ? (200, Html("Early title") + padding, null)
            : (200, padding + Html("Late title"), null));
        var service = CreateServiceAllowingOnlyLoopbackServer();

        (await service.FetchPreviewAsync($"{server.BaseUrl}/head-first")).Title.Should().Be("Early title");
        (await service.FetchPreviewAsync($"{server.BaseUrl}/tail")).Title.Should().BeNull();
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("93.184.216.34", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.255.255.255", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("192.168.0.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("::", false)]
    [InlineData("::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("64:ff9b::a9fe:a9fe", false)]
    [InlineData("2002:7f00:1::", false)]
    public void IsPublicAddress_ClassifiesAddresses(string address, bool expected)
    {
        PublicAddressHttpHandler.IsPublicAddress(IPAddress.Parse(address)).Should().Be(expected);
    }

    private static LinkPreviewService CreateServiceAllowingOnlyLoopbackServer() =>
        // 127.0.0.1 hosts the test server; everything else stays subject to the real public-address rule
        new(new HttpClient(PublicAddressHttpHandler.Create(a =>
            a.Equals(IPAddress.Loopback) || PublicAddressHttpHandler.IsPublicAddress(a))));

    private static string Html(string title) =>
        $"<html><head><meta property=\"og:title\" content=\"{title}\"></head><body></body></html>";

    private sealed class TestHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public string BaseUrl { get; }

        public TestHttpServer(Func<string, (int Status, string? Body, string? Location)> respond)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            BaseUrl = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add($"{BaseUrl}/");
            _listener.Start();
            _ = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try { context = await _listener.GetContextAsync(); }
                    catch { return; }

                    var (status, body, location) = respond(context.Request.Url!.AbsolutePath);
                    context.Response.StatusCode = status;
                    if (location != null) context.Response.RedirectLocation = location;
                    if (body != null)
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        var bytes = Encoding.UTF8.GetBytes(body);
                        try { await context.Response.OutputStream.WriteAsync(bytes); }
                        catch { /* client stopped reading at the size cap */ }
                    }
                    try { context.Response.Close(); } catch { }
                }
            });
        }

        public void Dispose() => _listener.Close();
    }
}

[Collection("Database")]
public class LinkPreviewHttpClientWiringTests
{
    private readonly DatabaseFixture _fixture;

    public LinkPreviewHttpClientWiringTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LinkPreviewHttpClient_FromApplicationServices_RefusesLoopback()
    {
        // The test factory mocks ILinkPreviewService, but the typed HttpClient registration from Program.cs remains
        var httpClientFactory = _fixture.Factory.Services.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient(nameof(ILinkPreviewService));

        var act = () => client.GetAsync("http://127.0.0.1:1/");

        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*does not resolve to a public address*");
    }
}
