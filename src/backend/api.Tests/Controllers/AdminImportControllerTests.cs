using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.ContentPackage.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminImportControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AdminImportControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task Import_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Array.Empty<byte>()), "file", "test.zip");

        var response = await unauthenticatedClient.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Validate_ValidPackage_ReturnsSuccess()
    {
        var zipBytes = CreateTestPackage("validate-test", "article", "draft");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.IsValid.Should().BeTrue();
        result.Data.Manifest.Should().NotBeNull();
        result.Data.Manifest!.ContentCount.Articles.Should().Be(1);
        result.Data.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithConflictingSlugs_ReportsConflicts()
    {
        // Use a slug that exists in seed data
        var zipBytes = CreateTestPackage(TestSeedData.PublishedArticle1Slug, "article", "published");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.Conflicts.Should().NotBeEmpty();
        result.Data.Conflicts.Should().Contain(c => c.Contains(TestSeedData.PublishedArticle1Slug));
    }

    [Fact]
    public async Task Validate_InvalidZip_ReturnsErrors()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NoFile_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Import_ValidPackage_CreatesArticle()
    {
        var uniqueSlug = $"import-test-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackage(uniqueSlug, "article", "draft");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.ArticlesImported.Should().Be(1);
        result.Data.Skipped.Should().Be(0);
        result.Data.Errors.Should().BeEmpty();

        // Cleanup - find and delete the imported article
        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task Import_WithSlugConflict_SkipsExisting()
    {
        var uniqueSlug = $"import-conflict-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackageMultiple(
            (TestSeedData.PublishedArticle1Slug, "article"),
            (uniqueSlug, "article")
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.ArticlesImported.Should().Be(1);
        result.Data.Skipped.Should().Be(1);
        result.Data.SkippedSlugs.Should().Contain(TestSeedData.PublishedArticle1Slug);

        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task Import_WithNewCategory_CreatesCategory()
    {
        var uniqueSlug = $"import-cat-{Guid.NewGuid():N}";
        var categoryName = $"NewCat-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackage(uniqueSlug, "article", "draft", categoryName: categoryName);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.CategoriesCreated.Should().Be(1);
        result.Data.ArticlesImported.Should().Be(1);

        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task Import_WithNewTags_CreatesTags()
    {
        var uniqueSlug = $"import-tag-{Guid.NewGuid():N}";
        var tagName = $"newtag-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackage(uniqueSlug, "article", "draft", tagNames: new[] { tagName });

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.TagsCreated.Should().Be(1);

        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task Import_Page_ImportsAsPage()
    {
        var uniqueSlug = $"import-page-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackage(uniqueSlug, "page", "published");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.PagesImported.Should().Be(1);
        result.Data.ArticlesImported.Should().Be(0);

        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task RoundTrip_ExportThenImport_PreservesData()
    {
        // Export specific article
        var exportResponse = await _client.PostAsJsonAsync("/api/admin/export", new
        {
            articleIds = new[] { TestSeedData.PublishedArticle1Id }
        });
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var exportBytes = await exportResponse.Content.ReadAsByteArrayAsync();

        // Parse the exported content to read it
        using var exportStream = new MemoryStream(exportBytes);
        using var exportArchive = new ZipArchive(exportStream, ZipArchiveMode.Read);
        var exportedEntry = exportArchive.Entries
            .First(e => e.FullName.Contains(TestSeedData.PublishedArticle1Slug));
        using var reader = new StreamReader(exportedEntry.Open());
        var originalContent = JsonSerializer.Deserialize<ContentEntryDto>(await reader.ReadToEndAsync(), JsonOptions);

        // Modify the slug so it doesn't conflict, then rebuild the ZIP
        var newSlug = $"roundtrip-{Guid.NewGuid():N}";
        var modifiedContent = originalContent! with { Slug = newSlug };

        var modifiedZipBytes = CreateZipFromContent(new[] { modifiedContent });

        // Import the modified package
        var importFormContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(modifiedZipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        importFormContent.Add(fileContent, "file", "test.zip");

        var importResponse = await _client.PostAsync("/api/admin/import", importFormContent);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var importResult = await importResponse.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        importResult!.Data.ArticlesImported.Should().Be(1);

        // Verify the imported content preserves data
        var verifyExport = await _client.PostAsJsonAsync("/api/admin/export", new { });
        var verifyBytes = await verifyExport.Content.ReadAsByteArrayAsync();
        using var verifyStream = new MemoryStream(verifyBytes);
        using var verifyArchive = new ZipArchive(verifyStream, ZipArchiveMode.Read);

        var importedEntry = verifyArchive.Entries.FirstOrDefault(e => e.FullName.Contains(newSlug));
        importedEntry.Should().NotBeNull();

        using var verifyReader = new StreamReader(importedEntry!.Open());
        var importedContent = JsonSerializer.Deserialize<ContentEntryDto>(await verifyReader.ReadToEndAsync(), JsonOptions);

        importedContent!.Title.Should().Be(originalContent.Title);
        importedContent.Content.Should().Be(originalContent.Content);
        importedContent.Category.Should().Be(originalContent.Category);
        importedContent.Status.Should().Be("published");

        await CleanupArticleBySlug(newSlug);
    }

    [Fact]
    public async Task Validate_WithTagsData_ReportsExistingTagWarnings()
    {
        var zipBytes = CreateTestPackageWithData(
            tags: new[] { new TagEntryDto("C#", TestSeedData.CSharpTagSlug) }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.IsValid.Should().BeTrue();
        result.Data.Warnings.Should().Contain(w => w.Contains("C#") && w.Contains("already exists"));
    }

    [Fact]
    public async Task Validate_WithSettingsData_ReportsSettingsWarning()
    {
        var zipBytes = CreateTestPackageWithData(
            settings: new Dictionary<string, string?> { { "site_name", "Test" }, { "new_key", "value" } }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import/validate", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.IsValid.Should().BeTrue();
        result.Data.Warnings.Should().Contain(w => w.Contains("2 site setting(s) will be imported"));
    }

    [Fact]
    public async Task Import_WithStandaloneTags_CreatesNewTags()
    {
        var uniqueTagSlug = $"pkg-tag-{Guid.NewGuid():N}";
        var uniqueTagName = $"PkgTag-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackageWithData(
            tags: new[] { new TagEntryDto(uniqueTagName, uniqueTagSlug) }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.TagsCreated.Should().BeGreaterThanOrEqualTo(1);

        await CleanupTagBySlug(uniqueTagSlug);
    }

    [Fact]
    public async Task Import_WithStandaloneCategories_CreatesNewCategories()
    {
        var uniqueCatSlug = $"pkg-cat-{Guid.NewGuid():N}";
        var uniqueCatName = $"PkgCat-{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackageWithData(
            categories: new[] { new CategoryEntryDto(uniqueCatName, uniqueCatSlug, "Test description") }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.CategoriesCreated.Should().BeGreaterThanOrEqualTo(1);

        await CleanupCategoryBySlug(uniqueCatSlug);
    }

    [Fact]
    public async Task Import_WithExistingTagSlugs_SkipsDuplicates()
    {
        var zipBytes = CreateTestPackageWithData(
            tags: new[]
            {
                new TagEntryDto("C#", TestSeedData.CSharpTagSlug),
                new TagEntryDto(".NET", TestSeedData.DotNetTagSlug)
            }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        // These tags already exist, so no new tags created from standalone data
        result!.Data.TagsCreated.Should().Be(0);
    }

    [Fact]
    public async Task Import_WithSettings_ImportsSiteSettings()
    {
        var uniqueKey = $"test_setting_{Guid.NewGuid():N}";
        var zipBytes = CreateTestPackageWithData(
            settings: new Dictionary<string, string?> { { uniqueKey, "test_value" } }
        );

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.SettingsImported.Should().Be(1);

        // Verify setting exists by exporting with settings
        var verifyResponse = await _client.PostAsJsonAsync("/api/admin/export", new { includeSettings = true });
        var verifyBytes = await verifyResponse.Content.ReadAsByteArrayAsync();
        using var verifyStream = new MemoryStream(verifyBytes);
        using var verifyArchive = new ZipArchive(verifyStream, ZipArchiveMode.Read);
        var settingsEntry = verifyArchive.GetEntry("data/settings.json");
        settingsEntry.Should().NotBeNull();
        using var reader = new StreamReader(settingsEntry!.Open());
        var settings = JsonSerializer.Deserialize<Dictionary<string, string?>>(await reader.ReadToEndAsync(), JsonOptions);
        settings.Should().ContainKey(uniqueKey);
        settings![uniqueKey].Should().Be("test_value");
    }

    [Fact]
    public async Task Import_LegacyV1Package_StillWorks()
    {
        var uniqueSlug = $"legacy-v1-{Guid.NewGuid():N}";

        // Build a v1.0 package with no data/ section
        var entry = new ContentEntryDto(
            Title: $"Test {uniqueSlug}",
            Slug: uniqueSlug,
            ContentType: "article",
            Status: "draft",
            Content: $"# Test\nContent for {uniqueSlug}",
            Layout: null,
            Excerpt: "Test excerpt",
            MetaTitle: null,
            MetaDescription: null,
            Category: null,
            Tags: null,
            Series: null,
            FeaturedImage: null,
            PublishedAt: null,
            CreatedAt: DateTime.UtcNow
        );

        var zipBytes = CreateZipFromContent(new[] { entry }, formatVersion: "1.0");

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");

        var response = await _client.PostAsync("/api/admin/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Data.ArticlesImported.Should().Be(1);
        result.Data.SettingsImported.Should().Be(0);

        await CleanupArticleBySlug(uniqueSlug);
    }

    [Fact]
    public async Task Import_MediaEntryEscapingUploadsDirectory_IsRejectedWithoutWritingFiles()
    {
        var marker = $"zipslip-{Guid.NewGuid():N}.txt";
        var escapedPath = Path.GetFullPath(Path.Combine(_fixture.Factory.UploadsPath, "..", marker));
        var zipBytes = CreateZipWithMedia(($"media/../{marker}", Encoding.UTF8.GetBytes("pwned")));

        var response = await PostPackage("/api/admin/import", zipBytes);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invalid media path");
        File.Exists(escapedPath).Should().BeFalse();
    }

    [Fact]
    public async Task Validate_MediaEntryEscapingUploadsDirectory_ReportsError()
    {
        var zipBytes = CreateZipWithMedia(("media/../../etc/evil.json", Encoding.UTF8.GetBytes("{}")));

        var response = await PostPackage("/api/admin/import/validate", zipBytes);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportValidationDto>>(JsonOptions);
        result!.Data.IsValid.Should().BeFalse();
        result.Data.Errors.Should().Contain(e => e.Contains("invalid media path"));
    }

    [Fact]
    public async Task Import_PackageExceedingDecompressedSizeCap_IsRejected()
    {
        using var factory = _fixture.Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ContentPackage:MaxDecompressedBytes", "4096"));
        var client = factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(client);
        var relativePath = $"2026/01/bomb-{Guid.NewGuid():N}.bin";
        var zipBytes = CreateZipWithMedia(($"media/{relativePath}", new byte[64 * 1024]));

        var response = await PostPackage("/api/admin/import", zipBytes, client);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("maximum decompressed size");
        File.Exists(Path.Combine(_fixture.Factory.UploadsPath, relativePath)).Should().BeFalse();
    }

    [Fact]
    public async Task Import_PackageWithMedia_WritesFileInsideUploadsDirectory()
    {
        var relativePath = $"2026/01/import-{Guid.NewGuid():N}.png";
        var zipBytes = CreateZipWithMedia(($"media/{relativePath}", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }));

        var response = await PostPackage("/api/admin/import", zipBytes);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result!.Data.MediaImported.Should().Be(1);
        result.Data.Errors.Should().BeEmpty();
        File.Exists(Path.Combine(_fixture.Factory.UploadsPath, relativePath)).Should().BeTrue();
    }

    [Fact]
    public async Task Import_MediaThatIsNotAnAllowedType_IsSkippedWithError()
    {
        var relativePath = $"2026/01/page-{Guid.NewGuid():N}.html";
        var zipBytes = CreateZipWithMedia(($"media/{relativePath}", Encoding.UTF8.GetBytes("<script>alert(1)</script>")));

        var response = await PostPackage("/api/admin/import", zipBytes);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>(JsonOptions);
        result!.Data.MediaImported.Should().Be(0);
        result.Data.Errors.Should().ContainSingle(e => e.Contains("file type is not allowed"));
        File.Exists(Path.Combine(_fixture.Factory.UploadsPath, relativePath)).Should().BeFalse();
    }

    private async Task<HttpResponseMessage> PostPackage(string url, byte[] zipBytes, HttpClient? client = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "test.zip");
        return await (client ?? _client).PostAsync(url, content);
    }

    private static byte[] CreateZipWithMedia(params (string EntryName, byte[] Data)[] media)
    {
        var baseZip = CreateZipFromContent(Array.Empty<ContentEntryDto>());
        using var memoryStream = new MemoryStream();
        memoryStream.Write(baseZip);
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, leaveOpen: true))
        {
            foreach (var (entryName, data) in media)
            {
                using var entryStream = archive.CreateEntry(entryName).Open();
                entryStream.Write(data);
            }
        }
        return memoryStream.ToArray();
    }

    private static byte[] CreateTestPackage(string slug, string contentType, string status,
        string? categoryName = null, string[]? tagNames = null, string? seriesName = null)
    {
        var entry = new ContentEntryDto(
            Title: $"Test {slug}",
            Slug: slug,
            ContentType: contentType,
            Status: status,
            Content: $"# Test\nContent for {slug}",
            Layout: null,
            Excerpt: "Test excerpt",
            MetaTitle: null,
            MetaDescription: null,
            Category: categoryName,
            Tags: tagNames,
            Series: seriesName != null ? new SeriesEntryDto(seriesName, 1) : null,
            FeaturedImage: null,
            PublishedAt: status == "published" ? DateTime.UtcNow : null,
            CreatedAt: DateTime.UtcNow
        );

        return CreateZipFromContent(new[] { entry });
    }

    private static byte[] CreateTestPackageMultiple(params (string slug, string contentType)[] items)
    {
        var entries = items.Select(item => new ContentEntryDto(
            Title: $"Test {item.slug}",
            Slug: item.slug,
            ContentType: item.contentType,
            Status: "draft",
            Content: $"# Test\nContent for {item.slug}",
            Layout: null,
            Excerpt: null,
            MetaTitle: null,
            MetaDescription: null,
            Category: null,
            Tags: null,
            Series: null,
            FeaturedImage: null,
            PublishedAt: null,
            CreatedAt: DateTime.UtcNow
        )).ToArray();

        return CreateZipFromContent(entries);
    }

    private static byte[] CreateTestPackageWithData(
        TagEntryDto[]? tags = null,
        CategoryEntryDto[]? categories = null,
        Dictionary<string, string?>? settings = null)
    {
        // No content entries — data-only package
        return CreateZipFromContent(Array.Empty<ContentEntryDto>(), tags: tags, categories: categories, settings: settings);
    }

    private static byte[] CreateZipFromContent(ContentEntryDto[] entries,
        TagEntryDto[]? tags = null,
        CategoryEntryDto[]? categories = null,
        Dictionary<string, string?>? settings = null,
        string formatVersion = "1.1")
    {
        var articleCount = entries.Count(e => e.ContentType == "article");
        var pageCount = entries.Count(e => e.ContentType == "page");

        var hasData = tags != null || categories != null || settings != null;
        ManifestDataCount? dataCount = hasData
            ? new ManifestDataCount(
                tags?.Length ?? 0,
                categories?.Length ?? 0,
                settings?.Count ?? 0)
            : null;

        var manifest = new ManifestDto(
            formatVersion,
            DateTime.UtcNow,
            "test",
            new ManifestContentCount(articleCount, pageCount),
            0,
            dataCount
        );

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write(JsonSerializer.Serialize(manifest, JsonOptions));
            }

            foreach (var entry in entries)
            {
                var folder = entry.ContentType == "page" ? "pages" : "articles";
                var zipEntry = archive.CreateEntry($"content/{folder}/{entry.Slug}.json");
                using var writer = new StreamWriter(zipEntry.Open());
                writer.Write(JsonSerializer.Serialize(entry, JsonOptions));
            }

            if (tags != null)
            {
                var zipEntry = archive.CreateEntry("data/tags.json");
                using var writer = new StreamWriter(zipEntry.Open());
                writer.Write(JsonSerializer.Serialize(tags, JsonOptions));
            }

            if (categories != null)
            {
                var zipEntry = archive.CreateEntry("data/categories.json");
                using var writer = new StreamWriter(zipEntry.Open());
                writer.Write(JsonSerializer.Serialize(categories, JsonOptions));
            }

            if (settings != null)
            {
                var zipEntry = archive.CreateEntry("data/settings.json");
                using var writer = new StreamWriter(zipEntry.Open());
                writer.Write(JsonSerializer.Serialize(settings, JsonOptions));
            }
        }

        return memoryStream.ToArray();
    }

    private async Task CleanupArticleBySlug(string slug)
    {
        // Get articles list and find the one with the matching slug
        var listResponse = await _client.GetAsync("/api/admin/articles?pageSize=100");
        if (listResponse.IsSuccessStatusCode)
        {
            var json = await listResponse.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var articles = doc.RootElement.GetProperty("data").EnumerateArray();
            foreach (var article in articles)
            {
                if (article.GetProperty("slug").GetString() == slug)
                {
                    var id = article.GetProperty("id").GetInt32();
                    await _client.DeleteAsync($"/api/admin/articles/{id}");
                    return;
                }
            }
        }

        // Also check pages
        var pagesResponse = await _client.GetAsync("/api/admin/pages?pageSize=100");
        if (pagesResponse.IsSuccessStatusCode)
        {
            var json = await pagesResponse.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var pages = doc.RootElement.GetProperty("data").EnumerateArray();
            foreach (var page in pages)
            {
                if (page.GetProperty("slug").GetString() == slug)
                {
                    var id = page.GetProperty("id").GetInt32();
                    await _client.DeleteAsync($"/api/admin/pages/{id}");
                    return;
                }
            }
        }
    }

    private async Task CleanupTagBySlug(string slug)
    {
        var response = await _client.GetAsync("/api/admin/tags");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var tags = doc.RootElement.GetProperty("data").EnumerateArray();
            foreach (var tag in tags)
            {
                if (tag.GetProperty("slug").GetString() == slug)
                {
                    var id = tag.GetProperty("id").GetInt32();
                    await _client.DeleteAsync($"/api/admin/tags/{id}");
                    return;
                }
            }
        }
    }

    private async Task CleanupCategoryBySlug(string slug)
    {
        var response = await _client.GetAsync("/api/admin/categories");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var categories = doc.RootElement.GetProperty("data").EnumerateArray();
            foreach (var category in categories)
            {
                if (category.GetProperty("slug").GetString() == slug)
                {
                    var id = category.GetProperty("id").GetInt32();
                    await _client.DeleteAsync($"/api/admin/categories/{id}");
                    return;
                }
            }
        }
    }
}
