using System.Net;
using System.Text;
using SharpQuill.Application.TweetPreviews.DTOs;
using SharpQuill.Application.TweetPreviews.Services;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// The real TweetPreviewService against a stubbed oEmbed endpoint (the API test factory mocks the service).
/// </summary>
public class TweetPreviewServiceTests
{
    private const string TweetUrl = "https://x.com/ann/status/123";

    private static (TweetPreviewService Service, StubHandler Handler) Create(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        return (new TweetPreviewService(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task FetchPreview_ParsesTheOEmbedResponse()
    {
        const string json = """
            {"author_name":"Ann Example","author_url":"https://twitter.com/ann",
             "html":"<blockquote class=\"twitter-tweet\"><p lang=\"en\">Hello &amp; welcome <a href=\"https://t.co/x\">#cms</a></p>&mdash; Ann (@ann) <a href=\"https://t.co/y\">ref</a> <a href=\"https://twitter.com/ann/status/123\">November 1, 2026</a></blockquote>"}
            """;
        var (service, handler) = Create(HttpStatusCode.OK, json);

        var preview = await service.FetchPreviewAsync(TweetUrl);

        handler.RequestUri!.ToString().Should().Be(
            "https://publish.twitter.com/oembed?url=https%3A%2F%2Fx.com%2Fann%2Fstatus%2F123&omit_script=true");
        preview.Should().BeEquivalentTo(new TweetPreviewDto("Ann Example", "@ann", "Hello & welcome #cms",
            "November 1, 2026", "https://unavatar.io/twitter/ann"));
    }

    [Fact]
    public async Task FetchPreview_MissingFields_GiveNulls()
    {
        var (service, _) = Create(HttpStatusCode.OK, "{}");

        (await service.FetchPreviewAsync(TweetUrl)).Should().BeEquivalentTo(new TweetPreviewDto(null, null, null, null, null));
    }

    [Fact]
    public async Task FetchPreview_AuthorUrlWithoutPathAndHtmlWithoutBlockquote_GiveNoHandleTextOrDate()
    {
        var (service, _) = Create(HttpStatusCode.OK,
            """{"author_name":"Ann","author_url":"https://twitter.com/","html":"<div>no quote</div>"}""");

        var preview = await service.FetchPreviewAsync(TweetUrl);

        preview.Should().BeEquivalentTo(new TweetPreviewDto("Ann", null, null, null, null));
    }

    [Fact]
    public async Task FetchPreview_UpstreamError_Throws()
    {
        var (service, _) = Create(HttpStatusCode.NotFound, "");

        await service.Invoking(s => s.FetchPreviewAsync(TweetUrl)).Should().ThrowAsync<HttpRequestException>();
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
