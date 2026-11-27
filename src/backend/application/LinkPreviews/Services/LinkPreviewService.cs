using System.Text;
using SharpQuill.Application.LinkPreviews.DTOs;
using HtmlAgilityPack;

namespace SharpQuill.Application.LinkPreviews.Services;

public class LinkPreviewService : ILinkPreviewService
{
    private readonly HttpClient _httpClient;

    // Meta tags live in <head>; there is no need to read (or buffer) more than this
    public const int MaxResponseBytes = 1024 * 1024;

    public LinkPreviewService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LinkPreviewDto> FetchPreviewAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Only absolute HTTP and HTTPS URLs can be previewed", nameof(url));
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; CMSBot/1.0)");
        request.Headers.Add("Accept", "text/html");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var html = await ReadLimitedAsync(response.Content, MaxResponseBytes);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = GetMetaContent(doc, "og:title")
            ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

        var description = GetMetaContent(doc, "og:description")
            ?? GetMetaByName(doc, "description");

        var imageUrl = GetMetaContent(doc, "og:image");

        var siteName = GetMetaContent(doc, "og:site_name");

        // Resolve relative image URLs
        if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(new Uri(url), imageUrl, out var absoluteUri))
                imageUrl = absoluteUri.ToString();
        }

        return new LinkPreviewDto(title, description, imageUrl, siteName);
    }

    private static async Task<string> ReadLimitedAsync(HttpContent content, int maxBytes)
    {
        await using var stream = await content.ReadAsStreamAsync();
        var buffer = new byte[maxBytes];
        var total = 0;
        int read;
        while (total < maxBytes && (read = await stream.ReadAsync(buffer.AsMemory(total))) > 0)
        {
            total += read;
        }

        return GetEncoding(content.Headers.ContentType?.CharSet).GetString(buffer, 0, total);
    }

    private static Encoding GetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
            return Encoding.UTF8;
        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static string? GetMetaContent(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode.SelectSingleNode(
            $"//meta[@property='{property}']");
        var value = node?.GetAttributeValue("content", "");
        return string.IsNullOrEmpty(value) ? null : System.Net.WebUtility.HtmlDecode(value);
    }

    private static string? GetMetaByName(HtmlDocument doc, string name)
    {
        var node = doc.DocumentNode.SelectSingleNode(
            $"//meta[@name='{name}']");
        var value = node?.GetAttributeValue("content", "");
        return string.IsNullOrEmpty(value) ? null : System.Net.WebUtility.HtmlDecode(value);
    }
}
