using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SharpQuill.Application.ContentPackage.DTOs;
using SharpQuill.Application.ContentPackage.Services;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Common;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;
using SharpQuill.Core.Settings;
using SharpQuill.Core.Tags;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// ContentPackageService with mocked repositories: the branches the HTTP round trips in AdminExportControllerTests
/// and AdminImportControllerTests do not reach (featured images and series on export, existing vs new categories,
/// tags and series, cached lookups, malformed entries and data files, repository failures).
/// </summary>
public sealed class ContentPackageServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

    private readonly string _uploads = Directory.CreateTempSubdirectory("cms-package-").FullName;
    private readonly Mock<IAdminArticleRepository> _articles = new();
    private readonly Mock<IArticleContentRepository> _contents = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IAdminCategoryRepository> _adminCategories = new();
    private readonly Mock<ITagRepository> _tags = new();
    private readonly Mock<IAdminTagRepository> _adminTags = new();
    private readonly Mock<ISeriesRepository> _series = new();
    private readonly Mock<IAdminSeriesRepository> _adminSeries = new();
    private readonly Mock<IMediaRepository> _media = new();
    private readonly Mock<ISiteSettingsRepository> _settings = new();
    private int _nextId = 100;

    public ContentPackageServiceTests()
    {
        _adminTags.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _articles.Setup(r => r.CreateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => { a.Id = _nextId++; return a; });
        _contents.Setup(r => r.CreateVersionAsync(It.IsAny<ArticleContent>()))
            .ReturnsAsync((ArticleContent c) => { c.Id = _nextId++; return c; });
        _adminCategories.Setup(r => r.CreateAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => { c.Id = _nextId++; return c; });
        _adminTags.Setup(r => r.CreateAsync(It.IsAny<Tag>())).ReturnsAsync((Tag t) => { t.Id = _nextId++; return t; });
        _adminSeries.Setup(r => r.CreateAsync(It.IsAny<ArticleSeries>()))
            .ReturnsAsync((ArticleSeries s) => { s.Id = _nextId++; return s; });
        _media.Setup(r => r.CreateAsync(It.IsAny<MediaItem>())).ReturnsAsync((MediaItem m) => { m.Id = _nextId++; return m; });
    }

    public void Dispose() => Directory.Delete(_uploads, recursive: true);

    private ContentPackageService Service(long maxBytes = ContentPackageService.DefaultMaxDecompressedBytes) => new(
        _articles.Object, _contents.Object, _categories.Object, _adminCategories.Object, _tags.Object, _adminTags.Object,
        _series.Object, _adminSeries.Object, _media.Object, _settings.Object, _uploads, maxBytes);

    private static ContentEntryDto Entry(string slug, string type = "article", string status = "draft", string? category = null,
        IReadOnlyList<string>? tags = null, SeriesEntryDto? series = null, MediaEntryDto? image = null) =>
        new("Title " + slug, slug, type, status, "Body", null, null, null, null, category, tags, series, image,
            null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    private static MemoryStream Package(params (string Name, object Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifest = new ManifestDto("1.1", DateTime.UtcNow, "cms-export", new ManifestContentCount(0, 0), 0, null);
            foreach (var (name, content) in entries.Prepend(("manifest.json", manifest)))
            {
                using var entry = zip.CreateEntry(name).Open();
                var bytes = content switch
                {
                    byte[] raw => raw,
                    string text => Encoding.UTF8.GetBytes(text),
                    _ => JsonSerializer.SerializeToUtf8Bytes(content, Json)
                };
                entry.Write(bytes);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static Dictionary<string, string> ReadZip(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => e.FullName, e => new StreamReader(e.Open()).ReadToEnd());
    }

    // --- Export ---

    [Fact]
    public async Task Export_IncludesFeaturedAndEmbeddedMediaSeriesAndTagNames()
    {
        Directory.CreateDirectory(Path.Combine(_uploads, "2026"));
        await File.WriteAllBytesAsync(Path.Combine(_uploads, "2026", "cover.png"), Png);
        await File.WriteAllBytesAsync(Path.Combine(_uploads, "2026", "inline.png"), Png);
        var tag = new Tag { Id = 1, Name = "Fresh name" };
        var article = new Article
        {
            Id = 1, Title = "A", Slug = "a", Status = ArticleStatus.Archived, FeaturedImageId = 5,
            Content = "![x](/uploads/2026/inline.png) and ![gone](/uploads/2026/missing.png)",
            Series = new ArticleSeries { Name = "Saga" }, SeriesOrder = null,
            ArticleTags = [new ArticleTag { TagId = 1, Tag = tag }, new ArticleTag { TagId = 2, Tag = new Tag { Name = "Joined" } }],
            Category = new Category { Name = "News" }
        };
        var page = new Article { Id = 2, Title = "P", Slug = "p", ContentType = ContentType.Page, Status = ArticleStatus.Published };
        _articles.Setup(r => r.GetAllAsync(It.IsAny<PaginationParams>(), null, null)).ReturnsAsync(([article, page], 2));
        _adminTags.Setup(r => r.GetAllAsync()).ReturnsAsync([tag]);
        _media.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(
            [new MediaItem { Id = 5, Path = "2026/cover.png", OriginalFilename = "cover.png", MimeType = "image/png", AltText = "Cover" }]);

        var (stream, error) = await Service().ExportAsync(new ExportRequestDto("all", null));

        error.Should().BeNull();
        var files = ReadZip(stream!);
        files.Keys.Should().BeEquivalentTo("manifest.json", "content/articles/a.json", "content/pages/p.json",
            "media/2026/cover.png", "media/2026/inline.png");
        var entry = JsonSerializer.Deserialize<ContentEntryDto>(files["content/articles/a.json"], Json)!;
        entry.Status.Should().Be("archived");
        entry.Category.Should().Be("News");
        entry.Series.Should().Be(new SeriesEntryDto("Saga", 0));
        entry.Tags.Should().Equal("Fresh name", "Joined");
        entry.FeaturedImage.Should().Be(new MediaEntryDto("2026/cover.png", "cover.png", "Cover", "image/png"));
        JsonSerializer.Deserialize<ContentEntryDto>(files["content/pages/p.json"], Json)!.Status.Should().Be("published");
        files["manifest.json"].Should().Contain("\"mediaCount\": 3", "the missing file is counted but not packed");
    }

    [Fact]
    public async Task Export_FilteredByIds_SkipsOtherArticlesAndUsesTheContentTypeFilter()
    {
        _articles.Setup(r => r.GetAllAsync(It.IsAny<PaginationParams>(), ContentType.Page, null)).ReturnsAsync(
            ([new Article { Id = 1, Slug = "one", ContentType = ContentType.Page }, new Article { Id = 2, Slug = "two", ContentType = ContentType.Page }], 2));

        var (stream, _) = await Service().ExportAsync(new ExportRequestDto("PAGE", [2]));

        ReadZip(stream!).Keys.Should().BeEquivalentTo("manifest.json", "content/pages/two.json");
        _media.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [Fact]
    public async Task Export_RepositoryFailure_ReturnsAnError()
    {
        _articles.Setup(r => r.GetAllAsync(It.IsAny<PaginationParams>(), It.IsAny<ContentType?>(), null))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var (stream, error) = await Service().ExportAsync(new ExportRequestDto(null, null));

        stream.Should().BeNull();
        error.Should().Be("Export failed: db down");
    }

    // --- Validate ---

    [Fact]
    public async Task Validate_ReportsWhatWillBeCreatedAndWhatExists()
    {
        _categories.Setup(r => r.GetBySlugAsync("old-cat")).ReturnsAsync(new Category());
        _tags.Setup(r => r.GetBySlugAsync("old-tag")).ReturnsAsync(new Tag());
        _series.Setup(r => r.GetBySlugAsync("old-saga")).ReturnsAsync(new ArticleSeries());
        using var package = Package(
            ("content/articles/a.json", Entry("a", category: "New Cat", tags: ["New Tag", "Old Tag"], series: new SeriesEntryDto("New Saga", 1))),
            ("content/articles/b.json", Entry("b", category: "Old Cat", series: new SeriesEntryDto("Old Saga", 2))),
            ("content/articles/null.json", "null"),
            ("content/articles/broken.json", "{not json"),
            ("data/categories.json", new[] { new CategoryEntryDto("Old", "old-cat", null), new CategoryEntryDto("New", "new-cat", null) }),
            ("data/settings.json", "{}"));

        var (validation, error) = await Service().ValidateAsync(package);

        error.Should().BeNull();
        validation!.IsValid.Should().BeFalse();
        validation.Warnings.Should().BeEquivalentTo(
            "Category 'New Cat' will be created", "Tag 'New Tag' will be created", "Series 'New Saga' will be created",
            "Category 'Old' already exists and will be skipped");
        validation.Errors.Should().ContainSingle().Which.Should().StartWith("Failed to parse content/articles/broken.json");
    }

    [Theory]
    [InlineData("data/tags.json")]
    [InlineData("data/categories.json")]
    [InlineData("data/settings.json")]
    public async Task Validate_MalformedDataFile_IsAnError(string name)
    {
        using var package = Package((name, "{oops"));

        var (validation, _) = await Service().ValidateAsync(package);

        validation!.Errors.Should().ContainSingle().Which.Should().StartWith($"Failed to parse {name}");
    }

    [Theory]
    [InlineData("null", "Failed to parse manifest.json")]
    [InlineData("{bad", "Invalid manifest.json format")]
    public async Task Validate_UnreadableManifest_IsInvalid(string manifest, string expected)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open());
            writer.Write(manifest);
        }
        stream.Position = 0;

        var (validation, _) = await Service().ValidateAsync(stream);

        validation!.Errors.Should().Equal(expected);
    }

    [Fact]
    public async Task Validate_OversizedPackage_IsAnError()
    {
        using var package = Package(("content/articles/a.json", Entry("a")));

        var (validation, _) = await Service(maxBytes: 10).ValidateAsync(package);

        validation!.Errors.Should().Contain("Package exceeds the maximum decompressed size");
    }

    [Fact]
    public async Task Validate_RepositoryFailureOutsideAnEntry_ReturnsAnError()
    {
        _tags.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("db down"));
        using var package = Package(("data/tags.json", new[] { new TagEntryDto("T", "t") }));

        var (validation, _) = await Service().ValidateAsync(package);

        validation!.Errors.Should().ContainSingle().Which.Should().Contain("db down");
    }

    [Fact]
    public async Task Validate_UnreadableStream_ReturnsAnError()
    {
        var (validation, error) = await Service().ValidateAsync(ClosedStream());

        validation.Should().BeNull();
        error.Should().StartWith("Validation failed:");
    }

    // --- Import ---

    [Fact]
    public async Task Import_ReusesExistingAndCachedCategoriesTagsAndSeries()
    {
        _categories.Setup(r => r.GetBySlugAsync("old-cat")).ReturnsAsync(new Category { Id = 1 });
        _tags.Setup(r => r.GetBySlugAsync("old-tag")).ReturnsAsync(new Tag { Id = 2 });
        _series.Setup(r => r.GetBySlugAsync("old-saga")).ReturnsAsync(new ArticleSeries { Id = 3 });
        using var package = Package(
            ("media/2026/cover.png", Png),
            ("content/articles/a.json", Entry("a", status: "published", category: "Old Cat", tags: ["Old Tag", "New Tag"],
                series: new SeriesEntryDto("Old Saga", 1), image: new MediaEntryDto("2026/cover.png", "cover.png", null, "image/png"))),
            ("content/articles/b.json", Entry("b", status: "archived", category: "New Cat", tags: ["new tag"],
                series: new SeriesEntryDto("New Saga", 1), image: new MediaEntryDto("2026/elsewhere.png", "x.png", null, "image/png"))),
            ("content/articles/c.json", Entry("c", category: "new cat", series: new SeriesEntryDto("new saga", 2))),
            ("content/articles/d.json", Entry("d", category: "Old Cat", series: new SeriesEntryDto("Old Saga", 3))),
            ("content/articles/null.json", "null"));

        var (result, error) = await Service().ImportAsync(package, authorId: 7);

        error.Should().BeNull();
        result!.ArticlesImported.Should().Be(4);
        result.MediaImported.Should().Be(1);
        result.CategoriesCreated.Should().Be(1, "'New Cat' is created once and found in the cache for 'new cat'");
        result.TagsCreated.Should().Be(1);
        result.SeriesCreated.Should().Be(1);
        result.Errors.Should().Equal("Failed to parse content/articles/null.json");
        _articles.Verify(r => r.CreateAsync(It.Is<Article>(a => a.Slug == "a" && a.CategoryId == 1 && a.FeaturedImageId != null
            && a.Status == ArticleStatus.Published && a.AuthorId == 7)));
        _articles.Verify(r => r.CreateAsync(It.Is<Article>(a => a.Slug == "b" && a.FeaturedImageId == null
            && a.Status == ArticleStatus.Archived)));
        _articles.Verify(r => r.SetPublishedVersionAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
        _adminSeries.Verify(r => r.SetArticleSeriesAsync(It.IsAny<int>(), 3, It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Import_ExistingMediaFile_IsKeptAndNotRecorded()
    {
        Directory.CreateDirectory(Path.Combine(_uploads, "2026"));
        await File.WriteAllBytesAsync(Path.Combine(_uploads, "2026", "cover.png"), [1, 2, 3]);
        using var package = Package(("media/2026/cover.png", Png));

        var (result, _) = await Service().ImportAsync(package, 1);

        result!.MediaImported.Should().Be(0);
        (await File.ReadAllBytesAsync(Path.Combine(_uploads, "2026", "cover.png"))).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Import_MediaRepositoryFailure_IsReportedPerFile()
    {
        _media.Setup(r => r.CreateAsync(It.IsAny<MediaItem>())).ThrowsAsync(new InvalidOperationException("db down"));
        using var package = Package(("media/a.png", Png));

        var (result, _) = await Service().ImportAsync(package, 1);

        result!.Errors.Should().Equal("Failed to import media 'media/a.png': db down");
    }

    [Fact]
    public async Task Import_ArticleRepositoryFailure_IsReportedPerEntry()
    {
        _articles.Setup(r => r.CreateAsync(It.IsAny<Article>())).ThrowsAsync(new InvalidOperationException("db down"));
        using var package = Package(("content/articles/a.json", Entry("a")));

        var (result, _) = await Service().ImportAsync(package, 1);

        result!.Errors.Should().Equal("Failed to import content/articles/a.json: db down");
    }

    [Fact]
    public async Task Import_DataFiles_CreateOnlyMissingEntriesAndSkipEmptySettings()
    {
        _categories.Setup(r => r.GetBySlugAsync("old")).ReturnsAsync(new Category());
        using var package = Package(
            ("data/categories.json", new[] { new CategoryEntryDto("Old", "old", null), new CategoryEntryDto("New", "new", "D") }),
            ("data/tags.json", "null"),
            ("data/settings.json", "{}"));

        var (result, _) = await Service().ImportAsync(package, 1);

        result!.CategoriesCreated.Should().Be(1);
        result.TagsCreated.Should().Be(0);
        result.SettingsImported.Should().Be(0);
        _settings.Verify(r => r.UpsertManyAsync(It.IsAny<IDictionary<string, string?>>()), Times.Never);
    }

    [Theory]
    [InlineData("data/tags.json")]
    [InlineData("data/categories.json")]
    [InlineData("data/settings.json")]
    public async Task Import_MalformedDataFile_IsReported(string name)
    {
        using var package = Package((name, "{oops"));

        var (result, _) = await Service().ImportAsync(package, 1);

        result!.Errors.Should().ContainSingle().Which.Should().StartWith($"Failed to import {name}");
    }

    [Fact]
    public async Task Import_MissingManifest_IsRefused()
    {
        var stream = new MemoryStream();
        using (new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) { }
        stream.Position = 0;

        (await Service().ImportAsync(stream, 1)).Should().Be(((ImportResultDto?)null, "Package is missing manifest.json"));
    }

    [Fact]
    public async Task Import_NotAZip_IsRefused()
    {
        var (_, error) = await Service().ImportAsync(new MemoryStream("not a zip"u8.ToArray()), 1);

        error.Should().Be("Invalid ZIP file");
    }

    [Fact]
    public async Task Import_UnreadableStream_ReturnsAnError()
    {
        var (_, error) = await Service().ImportAsync(ClosedStream(), 1);

        error.Should().StartWith("Import failed:");
    }

    /// <summary>A closed stream: ZipArchive refuses it with an error other than a ZIP format error.</summary>
    private static Stream ClosedStream()
    {
        var stream = new MemoryStream();
        stream.Dispose();
        return stream;
    }
}
