using System.Text.Json;
using SharpQuill.Application.TweetPreviews.DTOs;
using HtmlAgilityPack;

namespace SharpQuill.Application.TweetPreviews.Services;

public class TweetPreviewService : ITweetPreviewService
{
    private readonly HttpClient _httpClient;

    public TweetPreviewService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TweetPreviewDto> FetchPreviewAsync(string tweetUrl)
    {
        var oembedUrl = $"https://publish.twitter.com/oembed?url={Uri.EscapeDataString(tweetUrl)}&omit_script=true";

        using var response = await _httpClient.GetAsync(oembedUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var oembed = JsonDocument.Parse(json).RootElement;

        var authorName = oembed.TryGetProperty("author_name", out var an) ? an.GetString() : null;
        var authorUrl = oembed.TryGetProperty("author_url", out var au) ? au.GetString() : null;
        var html = oembed.TryGetProperty("html", out var h) ? h.GetString() : null;

        string? authorHandle = null;
        if (!string.IsNullOrEmpty(authorUrl))
        {
            var segments = new Uri(authorUrl).AbsolutePath.Trim('/');
            if (!string.IsNullOrEmpty(segments))
                authorHandle = $"@{segments}";
        }

        string? tweetText = null;
        string? tweetDate = null;
        string? profileUrl = null;

        if (!string.IsNullOrEmpty(html))
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Tweet text is in the first <p> inside the blockquote
            var pNode = doc.DocumentNode.SelectSingleNode("//blockquote/p");
            if (pNode != null)
                tweetText = System.Net.WebUtility.HtmlDecode(pNode.InnerText?.Trim());

            // Date is in the last <a> inside the blockquote (the permalink)
            var linkNodes = doc.DocumentNode.SelectNodes("//blockquote/a");
            if (linkNodes != null && linkNodes.Count > 0)
            {
                var lastLink = linkNodes[^1];
                tweetDate = System.Net.WebUtility.HtmlDecode(lastLink.InnerText?.Trim());
            }

            // Profile image URL from author_url — use unavatar.io for reliable avatars
            if (!string.IsNullOrEmpty(authorHandle))
            {
                var handle = authorHandle.TrimStart('@');
                profileUrl = $"https://unavatar.io/twitter/{handle}";
            }
        }

        return new TweetPreviewDto(authorName, authorHandle, tweetText, tweetDate, profileUrl);
    }
}
