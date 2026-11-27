using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Media.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminMediaControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AdminMediaControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task GetMedia_WithValidToken_ReturnsMediaList()
    {
        var response = await _client.GetAsync("/api/admin/media");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<MediaDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMedia_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/media");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadMedia_WithValidImage_ReturnsCreated()
    {
        var content = new MultipartFormDataContent();
        var imageBytes = CreateTestImage();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "test-image.png");

        var response = await _client.PostAsync("/api/admin/media", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data.OriginalFilename.Should().Be("test-image.png");
        result.Data.MimeType.Should().Be("image/png");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/media/{result.Data.Id}");
    }

    [Fact]
    public async Task UploadMedia_WithInvalidMimeType_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x00, 0x01, 0x02 };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "test-file.exe");

        var response = await _client.PostAsync("/api/admin/media", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_WithNoFile_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/admin/media", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMediaById_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/media/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMedia_WithValidId_ReturnsNoContent()
    {
        var content = new MultipartFormDataContent();
        var imageBytes = CreateTestImage();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "delete-test.png");

        var uploadResponse = await _client.PostAsync("/api/admin/media", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();
        var mediaId = uploaded!.Data.Id;

        var response = await _client.DeleteAsync($"/api/admin/media/{mediaId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/admin/media/{mediaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadMedia_HtmlDeclaredAsPng_ReturnsBadRequest()
    {
        var response = await UploadFile("<html><script>alert(1)</script></html>"u8.ToArray(), "image/png", "innocent.png");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_Svg_ReturnsBadRequest()
    {
        var svg = """<svg xmlns="http://www.w3.org/2000/svg"><script>alert(1)</script></svg>"""u8.ToArray();

        var response = await UploadFile(svg, "image/svg+xml", "logo.svg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_PngWithMisleadingNameAndType_IsStoredAsPng()
    {
        var response = await UploadFile(CreateTestImage(), "text/html", "page.html");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();
        result!.Data.MimeType.Should().Be("image/png");
        result.Data.Filename.Should().EndWith(".png");
        result.Data.Url.Should().EndWith(".png");

        await _client.DeleteAsync($"/api/admin/media/{result.Data.Id}");
    }

    [Fact]
    public async Task UploadedFile_IsServedWithNosniffHeader()
    {
        var uploadResponse = await UploadFile(CreateTestImage(), "image/png", "nosniff.png");
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();

        var response = await _fixture.Factory.CreateClient().GetAsync(uploaded!.Data.Url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");

        await _client.DeleteAsync($"/api/admin/media/{uploaded.Data.Id}");
    }

    [Fact]
    public async Task UploadedFile_IsServedSandboxed()
    {
        var uploadResponse = await UploadFile(CreateTestImage(), "image/png", "sandbox.png");
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();

        var response = await _fixture.Factory.CreateClient().GetAsync(uploaded!.Data.Url);

        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("sandbox").And.Contain("default-src 'none'");
        response.Content.Headers.ContentDisposition.Should().BeNull("images are shown inline");
    }

    [Fact]
    public async Task UploadedPdf_IsServedAsAttachment()
    {
        var pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n");
        var uploadResponse = await UploadFile(pdf, "application/pdf", "doc.pdf");
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>();

        var response = await _fixture.Factory.CreateClient().GetAsync(uploaded!.Data.Url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    }

    [Fact]
    public async Task DeleteMedia_Unused_RemovesOriginalAndVariantFiles()
    {
        var uploaded = await UploadImageAsync("files-test.png");
        var files = new[] { uploaded.Url, uploaded.MediumUrl, uploaded.ThumbnailUrl }
            .Where(url => url != null)
            .Select(url => ToDiskPath(url!))
            .ToList();
        files.Should().OnlyContain(f => File.Exists(f));

        var response = await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        files.Should().OnlyContain(f => !File.Exists(f));
    }

    [Fact]
    public async Task DeleteMedia_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/admin/media/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMedia_UsedAsFeaturedImage_ReturnsConflictAndKeepsMedia()
    {
        var uploaded = await UploadImageAsync("featured.png");
        var articleId = await CreateArticleAsync("Featured image article", featuredImageId: uploaded.Id);

        var response = await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = (await response.Content.ReadFromJsonAsync<JsonDocument>())!.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("MEDIA_IN_USE");
        var detail = error.GetProperty("details").EnumerateArray().Should().ContainSingle().Subject;
        detail.GetProperty("field").GetString().Should().Be("featured_image");
        detail.GetProperty("message").GetString().Should().Be("Featured image article");
        (await _client.GetAsync($"/api/admin/media/{uploaded.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        File.Exists(ToDiskPath(uploaded.Url)).Should().BeTrue();

        await DeleteArticleAsync(articleId);
    }

    [Fact]
    public async Task DeleteMedia_UsedInlineInContent_ReturnsConflict()
    {
        var uploaded = await UploadImageAsync("inline.png");
        // The medium variant URL counts as a use of the media, just like the original
        var url = uploaded.MediumUrl ?? uploaded.Url;
        var articleId = await CreateArticleAsync("Inline image article", content: $"Text\n\n![alt]({url})");

        var response = await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var usages = await GetUsagesAsync(uploaded.Id);
        usages.Should().ContainSingle().Which.Should().Be(new MediaUsageDto("content", articleId, "Inline image article", false));

        await DeleteArticleAsync(articleId);
    }

    [Fact]
    public async Task DeleteMedia_UsedByLayoutImageBlock_ReturnsConflict()
    {
        var uploaded = await UploadImageAsync("layout.png");
        var layout = new
        {
            Sections = new[]
            {
                new { Type = "full", Blocks = new object[] { new { Type = "image", MediaId = uploaded.Id, Alt = "x" } } }
            }
        };
        var articleId = await CreateArticleAsync("Layout image article", layout: layout);

        var response = await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var usages = await GetUsagesAsync(uploaded.Id);
        usages.Should().ContainSingle().Which.Kind.Should().Be("layout");

        await DeleteArticleAsync(articleId);
    }

    [Fact]
    public async Task DeleteMedia_ReferencedBySiteSetting_ReturnsConflict()
    {
        var uploaded = await UploadImageAsync("logo.png");

        var response = await TestSql.WithSiteSettingAsync("media-usage-test", uploaded.Url,
            () => _client.DeleteAsync($"/api/admin/media/{uploaded.Id}"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMedia_UsedWithForce_DeletesAndClearsFeaturedImage()
    {
        var uploaded = await UploadImageAsync("forced.png");
        var articleId = await CreateArticleAsync("Forced delete article", featuredImageId: uploaded.Id);

        var response = await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}?force=true");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/admin/media/{uploaded.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        File.Exists(ToDiskPath(uploaded.Url)).Should().BeFalse();
        var featured = await TestSql.ScalarAsync<object>("SELECT featured_image_id FROM articles WHERE id = @Id", ("Id", articleId));
        featured.Should().Be(DBNull.Value);

        await DeleteArticleAsync(articleId);
    }

    [Fact]
    public async Task GetMediaUsages_ListsPagesAndOnlyCurrentVersions()
    {
        var uploaded = await UploadImageAsync("versions.png");
        var pageId = await CreateArticleAsync("Usage page", content: $"![x]({uploaded.Url})", contentType: 1);
        // A newer version without the image: the older version is history and no longer counts
        var articleId = await CreateArticleAsync("Old version article", content: $"![x]({uploaded.Url})");
        await TestSql.ExecuteAsync(@"
            INSERT INTO article_contents (article_id, content, version_number, created_at)
            VALUES (@Id, 'No image any more', 2, NOW())", ("Id", articleId));

        var usages = await GetUsagesAsync(uploaded.Id);

        usages.Should().ContainSingle().Which.Should().Be(new MediaUsageDto("content", pageId, "Usage page", true));

        await DeleteArticleAsync(articleId);
        await DeleteArticleAsync(pageId);
        await _client.DeleteAsync($"/api/admin/media/{uploaded.Id}");
    }

    [Fact]
    public async Task GetMediaUsages_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/admin/media/99999/usages");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<MediaUploadResultDto> UploadImageAsync(string filename)
    {
        var response = await UploadFile(CreateTestImage(), "image/png", filename);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResultDto>>())!.Data;
    }

    private async Task<IReadOnlyList<MediaUsageDto>> GetUsagesAsync(int mediaId)
    {
        var response = await _client.GetAsync($"/api/admin/media/{mediaId}/usages");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<MediaUsageDto>>>())!.Data;
    }

    private async Task<int> CreateArticleAsync(string title, int? featuredImageId = null, string content = "Body",
        object? layout = null, int contentType = 0)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = title,
            Slug = $"media-usage-{Guid.NewGuid():N}",
            Content = layout == null ? content : null,
            Layout = layout,
            FeaturedImageId = featuredImageId,
            ContentType = contentType,
            Status = 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data.Id;
    }

    private Task DeleteArticleAsync(int articleId) =>
        TestSql.ExecuteAsync("DELETE FROM articles WHERE id = @Id", ("Id", articleId));

    private string ToDiskPath(string url) =>
        Path.Combine(_fixture.Factory.UploadsPath, url["/uploads/".Length..]);

    private async Task<HttpResponseMessage> UploadFile(byte[] bytes, string contentType, string filename)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", filename);
        return await _client.PostAsync("/api/admin/media", content);
    }

    private static byte[] CreateTestImage()
    {
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        };
    }
}
