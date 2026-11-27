using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.ContentPackage.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class AdminExportControllerTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminExportControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    [Fact]
    public async Task Export_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthenticatedClient = _fixture.Factory.CreateClient();

        var response = await unauthenticatedClient.PostAsJsonAsync("/api/admin/export", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_AllContent_ReturnsValidZip()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // Should have manifest
        var manifestEntry = archive.GetEntry("manifest.json");
        manifestEntry.Should().NotBeNull();

        using var reader = new StreamReader(manifestEntry!.Open());
        var manifestJson = await reader.ReadToEndAsync();
        var manifest = JsonSerializer.Deserialize<ManifestDto>(manifestJson, JsonOptions);
        manifest.Should().NotBeNull();
        manifest!.FormatVersion.Should().Be("1.1");
        manifest.ContentCount.Articles.Should().BeGreaterThan(0);

        // Should have content entries
        var contentEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
            .ToList();
        contentEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Export_ArticlesOnly_ContainsOnlyArticles()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { contentType = "article" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // All content entries should be in articles folder
        var contentEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
            .ToList();

        contentEntries.Should().NotBeEmpty();
        contentEntries.Should().AllSatisfy(e => e.FullName.Should().StartWith("content/articles/"));

        // Verify manifest counts
        var manifestEntry = archive.GetEntry("manifest.json");
        using var reader = new StreamReader(manifestEntry!.Open());
        var manifest = JsonSerializer.Deserialize<ManifestDto>(await reader.ReadToEndAsync(), JsonOptions);
        manifest!.ContentCount.Pages.Should().Be(0);
        manifest.ContentCount.Articles.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Export_PagesOnly_ContainsOnlyPages()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { contentType = "page" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var contentEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
            .ToList();

        contentEntries.Should().NotBeEmpty();
        contentEntries.Should().AllSatisfy(e => e.FullName.Should().StartWith("content/pages/"));
    }

    [Fact]
    public async Task Export_SpecificIds_ContainsOnlySelectedArticles()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new
        {
            articleIds = new[] { TestSeedData.PublishedArticle1Id }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var contentEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
            .ToList();

        contentEntries.Should().HaveCount(1);
        contentEntries[0].FullName.Should().Contain(TestSeedData.PublishedArticle1Slug);
    }

    [Fact]
    public async Task Export_VerifyContentEntryStructure()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new
        {
            articleIds = new[] { TestSeedData.PublishedArticle1Id }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entry = archive.Entries.First(e => e.FullName.Contains(TestSeedData.PublishedArticle1Slug));
        using var reader = new StreamReader(entry.Open());
        var contentEntry = JsonSerializer.Deserialize<ContentEntryDto>(await reader.ReadToEndAsync(), JsonOptions);

        contentEntry.Should().NotBeNull();
        contentEntry!.Title.Should().Be("Intro to C#");
        contentEntry.Slug.Should().Be(TestSeedData.PublishedArticle1Slug);
        contentEntry.ContentType.Should().Be("article");
        contentEntry.Status.Should().Be("published");
        contentEntry.Category.Should().Be("Technology");
        contentEntry.Tags.Should().NotBeNull();
        contentEntry.Tags.Should().Contain("C#");
        contentEntry.Series.Should().NotBeNull();
        contentEntry.Series!.Name.Should().Be("Getting Started");
        contentEntry.Series.Order.Should().Be(1);
    }

    [Fact]
    public async Task Export_WithIncludeTags_ContainsTagsData()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { includeTags = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var tagsEntry = archive.GetEntry("data/tags.json");
        tagsEntry.Should().NotBeNull();

        using var reader = new StreamReader(tagsEntry!.Open());
        var tags = JsonSerializer.Deserialize<TagEntryDto[]>(await reader.ReadToEndAsync(), JsonOptions);
        tags.Should().NotBeNull();
        tags!.Length.Should().BeGreaterThan(0);
        tags.Should().Contain(t => t.Slug == TestSeedData.CSharpTagSlug);
    }

    [Fact]
    public async Task Export_WithIncludeCategories_ContainsCategoriesData()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { includeCategories = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var categoriesEntry = archive.GetEntry("data/categories.json");
        categoriesEntry.Should().NotBeNull();

        using var reader = new StreamReader(categoriesEntry!.Open());
        var categories = JsonSerializer.Deserialize<CategoryEntryDto[]>(await reader.ReadToEndAsync(), JsonOptions);
        categories.Should().NotBeNull();
        categories!.Length.Should().BeGreaterThan(0);
        categories.Should().Contain(c => c.Slug == TestSeedData.TechnologySlug);

        // Verify description is included
        var techCategory = categories.First(c => c.Slug == TestSeedData.TechnologySlug);
        techCategory.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Export_WithIncludeSettings_ContainsSettingsData()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { includeSettings = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var settingsEntry = archive.GetEntry("data/settings.json");
        settingsEntry.Should().NotBeNull();

        using var reader = new StreamReader(settingsEntry!.Open());
        var settings = JsonSerializer.Deserialize<Dictionary<string, string?>>(await reader.ReadToEndAsync(), JsonOptions);
        settings.Should().NotBeNull();
        settings!.Count.Should().BeGreaterThan(0);
        settings.Should().ContainKey("site_name");
    }

    [Fact]
    public async Task Export_DefaultRequest_NoDataFolder()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var dataEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("data/"))
            .ToList();
        dataEntries.Should().BeEmpty();

        // Manifest should have no data count
        var manifestEntry = archive.GetEntry("manifest.json");
        using var reader = new StreamReader(manifestEntry!.Open());
        var manifest = JsonSerializer.Deserialize<ManifestDto>(await reader.ReadToEndAsync(), JsonOptions);
        manifest!.DataCount.Should().BeNull();
    }

    [Fact]
    public async Task Export_FormatVersion_Is1Point1_WithDataFlags()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/export", new { includeTags = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var manifestEntry = archive.GetEntry("manifest.json");
        using var reader = new StreamReader(manifestEntry!.Open());
        var manifest = JsonSerializer.Deserialize<ManifestDto>(await reader.ReadToEndAsync(), JsonOptions);
        manifest!.FormatVersion.Should().Be("1.1");
        manifest.DataCount.Should().NotBeNull();
        manifest.DataCount!.Tags.Should().BeGreaterThan(0);
    }
}
