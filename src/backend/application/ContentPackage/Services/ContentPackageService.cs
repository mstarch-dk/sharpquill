using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpQuill.Application.ContentPackage.DTOs;
using SharpQuill.Application.Media.Services;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;
using SharpQuill.Core.Settings;
using SharpQuill.Core.Tags;
using Article = SharpQuill.Core.Articles.Article;
using ArticleContent = SharpQuill.Core.Articles.ArticleContent;
using Category = SharpQuill.Core.Categories.Category;
using Tag = SharpQuill.Core.Tags.Tag;

namespace SharpQuill.Application.ContentPackage.Services;

public partial class ContentPackageService : IContentPackageService
{
    private readonly IAdminArticleRepository _articleRepository;
    private readonly IArticleContentRepository _contentRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAdminCategoryRepository _adminCategoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IAdminTagRepository _adminTagRepository;
    private readonly ISeriesRepository _seriesRepository;
    private readonly IAdminSeriesRepository _adminSeriesRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly ISiteSettingsRepository _settingsRepository;
    private readonly string _uploadsPath;
    private readonly long _maxDecompressedBytes;

    private const string FormatVersion = "1.1";
    public const long DefaultMaxDecompressedBytes = 500L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ContentPackageService(
        IAdminArticleRepository articleRepository,
        IArticleContentRepository contentRepository,
        ICategoryRepository categoryRepository,
        IAdminCategoryRepository adminCategoryRepository,
        ITagRepository tagRepository,
        IAdminTagRepository adminTagRepository,
        ISeriesRepository seriesRepository,
        IAdminSeriesRepository adminSeriesRepository,
        IMediaRepository mediaRepository,
        ISiteSettingsRepository settingsRepository,
        string uploadsPath,
        long maxDecompressedBytes = DefaultMaxDecompressedBytes)
    {
        _articleRepository = articleRepository;
        _contentRepository = contentRepository;
        _categoryRepository = categoryRepository;
        _adminCategoryRepository = adminCategoryRepository;
        _tagRepository = tagRepository;
        _adminTagRepository = adminTagRepository;
        _seriesRepository = seriesRepository;
        _adminSeriesRepository = adminSeriesRepository;
        _mediaRepository = mediaRepository;
        _settingsRepository = settingsRepository;
        _uploadsPath = Path.GetFullPath(uploadsPath);
        _maxDecompressedBytes = maxDecompressedBytes;
    }

    public async Task<(Stream? Stream, string? Error)> ExportAsync(ExportRequestDto request)
    {
        try
        {
            // Parse content type from request
            ContentType? contentType = request.ContentType?.ToLowerInvariant() switch
            {
                "article" => ContentType.Article,
                "page" => ContentType.Page,
                _ => null
            };

            // Load all articles (use large page to get all)
            var pagination = new PaginationParams { Page = 1, PageSize = 10000 };
            var (articles, _) = await _articleRepository.GetAllAsync(pagination, contentType);
            var articleList = articles.ToList();

            // Filter by IDs if specified
            var idList = request.ArticleIds?.ToList();
            if (idList is { Count: > 0 })
            {
                articleList = articleList.Where(a => idList.Contains(a.Id)).ToList();
            }

            // Collect featured image IDs for batch lookup
            var featuredImageIds = articleList
                .Where(a => a.FeaturedImageId.HasValue)
                .Select(a => a.FeaturedImageId!.Value)
                .Distinct()
                .ToList();

            var mediaLookup = new Dictionary<int, MediaItem>();
            if (featuredImageIds.Count > 0)
            {
                var mediaEntities = await _mediaRepository.GetByIdsAsync(featuredImageIds);
                mediaLookup = mediaEntities.ToDictionary(m => m.Id);
            }

            // Load all tags for name lookup (article repo may not populate tag names)
            var allTags = await _adminTagRepository.GetAllAsync();
            var tagLookup = allTags.ToDictionary(t => t.Id, t => t.Name);

            // Scan markdown content for embedded media paths
            var embeddedMediaPaths = new HashSet<string>();
            foreach (var article in articleList)
            {
                if (!string.IsNullOrEmpty(article.Content))
                {
                    foreach (Match match in UploadsPathRegex().Matches(article.Content))
                    {
                        embeddedMediaPaths.Add(match.Groups[1].Value);
                    }
                }
            }

            // Map articles to content entries
            var contentEntries = articleList.Select(a => MapToContentEntry(a, mediaLookup, tagLookup)).ToList();

            // Collect all media files (featured images + embedded)
            var allMediaPaths = new HashSet<string>();
            foreach (var entry in contentEntries)
            {
                if (entry.FeaturedImage != null)
                {
                    allMediaPaths.Add(entry.FeaturedImage.Path);
                }
            }
            foreach (var path in embeddedMediaPaths)
            {
                allMediaPaths.Add(path);
            }

            // Count articles vs pages
            var articleCount = contentEntries.Count(e => e.ContentType == "article");
            var pageCount = contentEntries.Count(e => e.ContentType == "page");

            // Prepare standalone data exports
            TagEntryDto[]? tagEntries = null;
            CategoryEntryDto[]? categoryEntries = null;
            Dictionary<string, string?>? settingsEntries = null;

            if (request.IncludeTags)
            {
                var tags = allTags.ToList();
                tagEntries = tags.Select(t => new TagEntryDto(t.Name, t.Slug)).ToArray();
            }

            if (request.IncludeCategories)
            {
                var categories = await _adminCategoryRepository.GetAllAsync();
                categoryEntries = categories.Select(c => new CategoryEntryDto(c.Name, c.Slug, c.Description)).ToArray();
            }

            if (request.IncludeSettings)
            {
                var settings = await _settingsRepository.GetAllAsync();
                settingsEntries = settings.ToDictionary(s => s.Key, s => s.Value);
            }

            var hasData = tagEntries != null || categoryEntries != null || settingsEntries != null;
            ManifestDataCount? dataCount = hasData
                ? new ManifestDataCount(
                    tagEntries?.Length ?? 0,
                    categoryEntries?.Length ?? 0,
                    settingsEntries?.Count ?? 0)
                : null;

            var manifest = new ManifestDto(
                FormatVersion,
                DateTime.UtcNow,
                "cms-export",
                new ManifestContentCount(articleCount, pageCount),
                allMediaPaths.Count,
                dataCount
            );

            // Build ZIP
            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Write manifest
                var manifestEntry = archive.CreateEntry("manifest.json");
                await using (var writer = new StreamWriter(manifestEntry.Open()))
                {
                    await writer.WriteAsync(JsonSerializer.Serialize(manifest, JsonOptions));
                }

                // Write content entries
                foreach (var entry in contentEntries)
                {
                    var folder = entry.ContentType == "page" ? "pages" : "articles";
                    var zipEntry = archive.CreateEntry($"content/{folder}/{entry.Slug}.json");
                    await using var writer = new StreamWriter(zipEntry.Open());
                    await writer.WriteAsync(JsonSerializer.Serialize(entry, JsonOptions));
                }

                // Copy media files
                foreach (var mediaPath in allMediaPaths)
                {
                    var filePath = Path.Combine(_uploadsPath, mediaPath);
                    if (File.Exists(filePath))
                    {
                        var zipEntry = archive.CreateEntry($"media/{mediaPath}");
                        await using var entryStream = zipEntry.Open();
                        await using var fileStream = File.OpenRead(filePath);
                        await fileStream.CopyToAsync(entryStream);
                    }
                }

                // Write standalone data files
                if (tagEntries != null)
                {
                    var zipEntry = archive.CreateEntry("data/tags.json");
                    await using var writer = new StreamWriter(zipEntry.Open());
                    await writer.WriteAsync(JsonSerializer.Serialize(tagEntries, JsonOptions));
                }

                if (categoryEntries != null)
                {
                    var zipEntry = archive.CreateEntry("data/categories.json");
                    await using var writer = new StreamWriter(zipEntry.Open());
                    await writer.WriteAsync(JsonSerializer.Serialize(categoryEntries, JsonOptions));
                }

                if (settingsEntries != null)
                {
                    var zipEntry = archive.CreateEntry("data/settings.json");
                    await using var writer = new StreamWriter(zipEntry.Open());
                    await writer.WriteAsync(JsonSerializer.Serialize(settingsEntries, JsonOptions));
                }
            }

            memoryStream.Position = 0;
            return (memoryStream, null);
        }
        catch (Exception ex)
        {
            return (null, $"Export failed: {ex.Message}");
        }
    }

    public async Task<(ImportValidationDto? Validation, string? Error)> ValidateAsync(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            // Check manifest
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
            {
                return (new ImportValidationDto(false, null, [], [], ["Package is missing manifest.json"]), null);
            }

            ManifestDto? manifest;
            try
            {
                using var reader = new StreamReader(manifestEntry.Open());
                var json = await reader.ReadToEndAsync();
                manifest = JsonSerializer.Deserialize<ManifestDto>(json, JsonOptions);
                if (manifest == null)
                {
                    return (new ImportValidationDto(false, null, [], [], ["Failed to parse manifest.json"]), null);
                }
            }
            catch
            {
                return (new ImportValidationDto(false, null, [], [], ["Invalid manifest.json format"]), null);
            }

            var conflicts = new List<string>();
            var warnings = new List<string>();
            var errors = new List<string>();

            var archiveError = CheckArchiveSafety(archive);
            if (archiveError != null)
            {
                errors.Add(archiveError);
            }

            // Parse content entries and check for conflicts
            var contentEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
                .ToList();

            foreach (var entry in contentEntries)
            {
                try
                {
                    using var reader = new StreamReader(entry.Open());
                    var json = await reader.ReadToEndAsync();
                    var contentEntry = JsonSerializer.Deserialize<ContentEntryDto>(json, JsonOptions);
                    if (contentEntry == null) continue;

                    // Check slug conflicts
                    if (await _articleRepository.SlugExistsAsync(contentEntry.Slug))
                    {
                        conflicts.Add($"Slug '{contentEntry.Slug}' already exists and will be skipped");
                    }

                    // Check if category needs to be created
                    if (!string.IsNullOrEmpty(contentEntry.Category))
                    {
                        var category = await _categoryRepository.GetBySlugAsync(ToSlug(contentEntry.Category));
                        if (category == null)
                        {
                            warnings.Add($"Category '{contentEntry.Category}' will be created");
                        }
                    }

                    // Check if tags need to be created
                    if (contentEntry.Tags != null)
                    {
                        foreach (var tagName in contentEntry.Tags)
                        {
                            var tag = await _tagRepository.GetBySlugAsync(ToSlug(tagName));
                            if (tag == null)
                            {
                                warnings.Add($"Tag '{tagName}' will be created");
                            }
                        }
                    }

                    // Check if series needs to be created
                    if (contentEntry.Series != null)
                    {
                        var series = await _seriesRepository.GetBySlugAsync(ToSlug(contentEntry.Series.Name));
                        if (series == null)
                        {
                            warnings.Add($"Series '{contentEntry.Series.Name}' will be created");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to parse {entry.FullName}: {ex.Message}");
                }
            }

            // Validate standalone data entries
            var tagsDataEntry = archive.GetEntry("data/tags.json");
            if (tagsDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(tagsDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var tagEntries = JsonSerializer.Deserialize<TagEntryDto[]>(json, JsonOptions);
                    if (tagEntries != null)
                    {
                        foreach (var tagEntry in tagEntries)
                        {
                            var existing = await _tagRepository.GetBySlugAsync(tagEntry.Slug);
                            if (existing != null)
                            {
                                warnings.Add($"Tag '{tagEntry.Name}' already exists and will be skipped");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to parse data/tags.json: {ex.Message}");
                }
            }

            var categoriesDataEntry = archive.GetEntry("data/categories.json");
            if (categoriesDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(categoriesDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var categoryEntries = JsonSerializer.Deserialize<CategoryEntryDto[]>(json, JsonOptions);
                    if (categoryEntries != null)
                    {
                        foreach (var catEntry in categoryEntries)
                        {
                            var existing = await _categoryRepository.GetBySlugAsync(catEntry.Slug);
                            if (existing != null)
                            {
                                warnings.Add($"Category '{catEntry.Name}' already exists and will be skipped");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to parse data/categories.json: {ex.Message}");
                }
            }

            var settingsDataEntry = archive.GetEntry("data/settings.json");
            if (settingsDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(settingsDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var settingsEntries = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions);
                    if (settingsEntries != null && settingsEntries.Count > 0)
                    {
                        warnings.Add($"{settingsEntries.Count} site setting(s) will be imported (existing values will be overwritten)");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to parse data/settings.json: {ex.Message}");
                }
            }

            // Deduplicate warnings
            warnings = warnings.Distinct().ToList();

            var isValid = errors.Count == 0;
            return (new ImportValidationDto(isValid, manifest, conflicts, warnings, errors), null);
        }
        catch (InvalidDataException)
        {
            return (new ImportValidationDto(false, null, [], [], ["Invalid ZIP file"]), null);
        }
        catch (Exception ex)
        {
            return (null, $"Validation failed: {ex.Message}");
        }
    }

    public async Task<(ImportResultDto? Result, string? Error)> ImportAsync(Stream stream, int authorId)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
            {
                return (null, "Package is missing manifest.json");
            }

            int articlesImported = 0, pagesImported = 0, mediaImported = 0;
            int categoriesCreated = 0, tagsCreated = 0, seriesCreated = 0, settingsImported = 0;
            var skippedSlugs = new List<string>();
            var importErrors = new List<string>();

            // Caches for find-or-create lookups
            var categoryCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tagCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var seriesCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mediaPathCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Reject the whole package before writing anything if it is oversized or would escape the uploads root
            var archiveError = CheckArchiveSafety(archive);
            if (archiveError != null)
            {
                return (null, archiveError);
            }

            // Import media files first
            var mediaEntries = GetMediaEntries(archive);
            var remainingBytes = _maxDecompressedBytes;

            foreach (var mediaEntry in mediaEntries)
            {
                try
                {
                    // media/2024/01/filename.jpg -> 2024/01/filename.jpg
                    var relativePath = mediaEntry.FullName["media/".Length..];
                    var fullPath = ResolveMediaPath(relativePath)!;

                    if (File.Exists(fullPath))
                    {
                        // File already exists, skip but don't error
                        continue;
                    }

                    // Declared entry sizes can be forged, so enforce the budget on the bytes actually written
                    DetectedMediaType? detected;
                    long written = -1;
                    await using (var entryStream = mediaEntry.Open())
                    {
                        // Only accept known-safe media whose extension matches its content, as these are served from /uploads
                        var header = await MediaTypeDetector.ReadHeaderAsync(entryStream);
                        detected = MediaTypeDetector.Detect(header);
                        if (detected != null && detected.AcceptedExtensions.Contains(Path.GetExtension(fullPath).ToLowerInvariant()))
                        {
                            var directory = Path.GetDirectoryName(fullPath);
                            if (directory != null) Directory.CreateDirectory(directory);

                            await using var fileStream = new FileStream(fullPath, FileMode.Create);
                            await fileStream.WriteAsync(header);
                            var rest = await CopyWithLimitAsync(entryStream, fileStream, remainingBytes - header.Length);
                            written = rest < 0 ? -1 : header.Length + rest;
                        }
                        else
                        {
                            detected = null;
                        }
                    }

                    if (detected == null)
                    {
                        importErrors.Add($"Skipped media '{mediaEntry.FullName}': file type is not allowed or does not match its extension");
                        continue;
                    }

                    if (written < 0)
                    {
                        File.Delete(fullPath);
                        return (null, "Package exceeds the maximum decompressed size");
                    }
                    remainingBytes -= written;

                    // Create media DB record
                    var media = new MediaItem
                    {
                        Filename = mediaEntry.Name,
                        OriginalFilename = mediaEntry.Name,
                        MimeType = detected.MimeType,
                        Size = written,
                        Path = relativePath.Replace("\\", "/"),
                        AuthorId = authorId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var created = await _mediaRepository.CreateAsync(media);
                    mediaPathCache[relativePath.Replace("\\", "/")] = created.Id;
                    mediaImported++;
                }
                catch (Exception ex)
                {
                    importErrors.Add($"Failed to import media '{mediaEntry.FullName}': {ex.Message}");
                }
            }

            // Parse and import content entries
            var contentEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("content/") && e.FullName.EndsWith(".json"))
                .ToList();

            foreach (var entry in contentEntries)
            {
                try
                {
                    using var reader = new StreamReader(entry.Open());
                    var json = await reader.ReadToEndAsync();
                    var contentEntry = JsonSerializer.Deserialize<ContentEntryDto>(json, JsonOptions);
                    if (contentEntry == null)
                    {
                        importErrors.Add($"Failed to parse {entry.FullName}");
                        continue;
                    }

                    // Skip if slug exists
                    if (await _articleRepository.SlugExistsAsync(contentEntry.Slug))
                    {
                        skippedSlugs.Add(contentEntry.Slug);
                        continue;
                    }

                    // Resolve category
                    int? categoryId = null;
                    if (!string.IsNullOrEmpty(contentEntry.Category))
                    {
                        var (catId, catCreated) = await FindOrCreateCategoryAsync(contentEntry.Category, categoryCache);
                        categoryId = catId;
                        if (catCreated) categoriesCreated++;
                    }

                    // Resolve tags
                    var tagIds = new List<int>();
                    if (contentEntry.Tags != null)
                    {
                        foreach (var tagName in contentEntry.Tags)
                        {
                            var (tagId, created) = await FindOrCreateTagAsync(tagName, tagCache);
                            tagIds.Add(tagId);
                            if (created) tagsCreated++;
                        }
                    }

                    // Resolve series
                    int? seriesId = null;
                    int? seriesOrder = null;
                    if (contentEntry.Series != null)
                    {
                        var (sId, created) = await FindOrCreateSeriesAsync(contentEntry.Series.Name, seriesCache);
                        seriesId = sId;
                        seriesOrder = contentEntry.Series.Order;
                        if (created) seriesCreated++;
                    }

                    // Resolve featured image
                    int? featuredImageId = null;
                    if (contentEntry.FeaturedImage != null && mediaPathCache.TryGetValue(contentEntry.FeaturedImage.Path, out var mediaId))
                    {
                        featuredImageId = mediaId;
                    }

                    // Parse content type and status
                    var isPage = contentEntry.ContentType == "page";
                    var coreContentType = isPage ? ContentType.Page : ContentType.Article;
                    var status = contentEntry.Status switch
                    {
                        "published" => ArticleStatus.Published,
                        "archived" => ArticleStatus.Archived,
                        _ => ArticleStatus.Draft
                    };

                    // Create article
                    var article = new Article
                    {
                        Title = contentEntry.Title,
                        Slug = contentEntry.Slug,
                        ContentType = coreContentType,
                        Status = status,
                        AuthorId = isPage ? null : authorId,
                        CategoryId = categoryId,
                        FeaturedImageId = featuredImageId,
                        PublishedAt = contentEntry.PublishedAt,
                        CreatedAt = contentEntry.CreatedAt
                    };

                    var createdArticle = await _articleRepository.CreateAsync(article);

                    // Create content version
                    var versionNumber = await _contentRepository.GetNextVersionNumberAsync(createdArticle.Id);
                    var articleContent = new ArticleContent
                    {
                        ArticleId = createdArticle.Id,
                        Content = contentEntry.Content,
                        Excerpt = contentEntry.Excerpt,
                        MetaTitle = contentEntry.MetaTitle,
                        MetaDescription = contentEntry.MetaDescription,
                        Layout = contentEntry.Layout,
                        VersionNumber = versionNumber,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdVersion = await _contentRepository.CreateVersionAsync(articleContent);

                    // If published, set published version
                    if (status == ArticleStatus.Published)
                    {
                        await _articleRepository.SetPublishedVersionAsync(createdArticle.Id, createdVersion.Id);
                    }

                    // Assign tags
                    if (tagIds.Count > 0)
                    {
                        await _articleRepository.UpdateTagsAsync(createdArticle.Id, tagIds);
                    }

                    // Set series
                    if (seriesId.HasValue && seriesOrder.HasValue)
                    {
                        await _adminSeriesRepository.SetArticleSeriesAsync(createdArticle.Id, seriesId.Value, seriesOrder.Value);
                    }

                    if (isPage) pagesImported++;
                    else articlesImported++;
                }
                catch (Exception ex)
                {
                    importErrors.Add($"Failed to import {entry.FullName}: {ex.Message}");
                }
            }

            // Import standalone data entries
            var tagsDataEntry = archive.GetEntry("data/tags.json");
            if (tagsDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(tagsDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var tagEntries = JsonSerializer.Deserialize<TagEntryDto[]>(json, JsonOptions);
                    if (tagEntries != null)
                    {
                        foreach (var tagEntry in tagEntries)
                        {
                            var existing = await _tagRepository.GetBySlugAsync(tagEntry.Slug);
                            if (existing == null)
                            {
                                await _adminTagRepository.CreateAsync(new Tag
                                {
                                    Name = tagEntry.Name,
                                    Slug = tagEntry.Slug,
                                    CreatedAt = DateTime.UtcNow
                                });
                                tagsCreated++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    importErrors.Add($"Failed to import data/tags.json: {ex.Message}");
                }
            }

            var categoriesDataEntry = archive.GetEntry("data/categories.json");
            if (categoriesDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(categoriesDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var categoryEntries = JsonSerializer.Deserialize<CategoryEntryDto[]>(json, JsonOptions);
                    if (categoryEntries != null)
                    {
                        foreach (var catEntry in categoryEntries)
                        {
                            var existing = await _categoryRepository.GetBySlugAsync(catEntry.Slug);
                            if (existing == null)
                            {
                                await _adminCategoryRepository.CreateAsync(new Category
                                {
                                    Name = catEntry.Name,
                                    Slug = catEntry.Slug,
                                    Description = catEntry.Description,
                                    CreatedAt = DateTime.UtcNow
                                });
                                categoriesCreated++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    importErrors.Add($"Failed to import data/categories.json: {ex.Message}");
                }
            }

            var settingsDataEntry = archive.GetEntry("data/settings.json");
            if (settingsDataEntry != null)
            {
                try
                {
                    using var reader = new StreamReader(settingsDataEntry.Open());
                    var json = await reader.ReadToEndAsync();
                    var settingsEntries = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions);
                    if (settingsEntries != null && settingsEntries.Count > 0)
                    {
                        await _settingsRepository.UpsertManyAsync(settingsEntries);
                        settingsImported = settingsEntries.Count;
                    }
                }
                catch (Exception ex)
                {
                    importErrors.Add($"Failed to import data/settings.json: {ex.Message}");
                }
            }

            var result = new ImportResultDto(
                articlesImported, pagesImported, mediaImported,
                categoriesCreated, tagsCreated, seriesCreated,
                settingsImported,
                skippedSlugs.Count, skippedSlugs, importErrors
            );

            return (result, null);
        }
        catch (InvalidDataException)
        {
            return (null, "Invalid ZIP file");
        }
        catch (Exception ex)
        {
            return (null, $"Import failed: {ex.Message}");
        }
    }

    private ContentEntryDto MapToContentEntry(Article article, Dictionary<int, MediaItem> mediaLookup, Dictionary<int, string> tagLookup)
    {
        MediaEntryDto? featuredImage = null;
        if (article.FeaturedImageId.HasValue && mediaLookup.TryGetValue(article.FeaturedImageId.Value, out var media))
        {
            featuredImage = new MediaEntryDto(
                media.Path,
                media.OriginalFilename,
                media.AltText,
                media.MimeType
            );
        }

        SeriesEntryDto? series = null;
        if (article.Series != null)
        {
            series = new SeriesEntryDto(article.Series.Name, article.SeriesOrder ?? 0);
        }

        var tags = article.ArticleTags
            .Select(at => tagLookup.TryGetValue(at.TagId, out var name) ? name : at.Tag.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        var contentType = article.ContentType == ContentType.Page ? "page" : "article";
        var status = article.Status switch
        {
            ArticleStatus.Published => "published",
            ArticleStatus.Archived => "archived",
            _ => "draft"
        };

        return new ContentEntryDto(
            article.Title,
            article.Slug,
            contentType,
            status,
            article.Content,
            article.Layout,
            article.Excerpt,
            article.MetaTitle,
            article.MetaDescription,
            article.Category?.Name,
            tags.Count > 0 ? tags : null,
            series,
            featuredImage,
            article.PublishedAt,
            article.CreatedAt
        );
    }

    private async Task<(int? Id, bool Created)> FindOrCreateCategoryAsync(string name, Dictionary<string, int> cache)
    {
        if (cache.TryGetValue(name, out var cachedId))
        {
            return (cachedId, false);
        }

        var slug = ToSlug(name);
        var existing = await _categoryRepository.GetBySlugAsync(slug);
        if (existing != null)
        {
            cache[name] = existing.Id;
            return (existing.Id, false);
        }

        var created = await _adminCategoryRepository.CreateAsync(new Category
        {
            Name = name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        });

        cache[name] = created.Id;
        return (created.Id, true);
    }

    private async Task<(int Id, bool Created)> FindOrCreateTagAsync(string name, Dictionary<string, int> cache)
    {
        if (cache.TryGetValue(name, out var cachedId))
        {
            return (cachedId, false);
        }

        var slug = ToSlug(name);
        var existing = await _tagRepository.GetBySlugAsync(slug);
        if (existing != null)
        {
            cache[name] = existing.Id;
            return (existing.Id, false);
        }

        var created = await _adminTagRepository.CreateAsync(new Tag
        {
            Name = name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        });

        cache[name] = created.Id;
        return (created.Id, true);
    }

    private async Task<(int Id, bool Created)> FindOrCreateSeriesAsync(string name, Dictionary<string, int> cache)
    {
        if (cache.TryGetValue(name, out var cachedId))
        {
            return (cachedId, false);
        }

        var slug = ToSlug(name);
        var existing = await _seriesRepository.GetBySlugAsync(slug);
        if (existing != null)
        {
            cache[name] = existing.Id;
            return (existing.Id, false);
        }

        var created = await _adminSeriesRepository.CreateAsync(new ArticleSeries
        {
            Name = name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        });

        cache[name] = created.Id;
        return (created.Id, true);
    }

    private static string ToSlug(string name)
    {
        return SlugRegex().Replace(name.ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex(@"/uploads/(\S+?)(?=[)\s""']|$)")]
    private static partial Regex UploadsPathRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();

    private static List<ZipArchiveEntry> GetMediaEntries(ZipArchive archive) =>
        archive.Entries
            .Where(e => e.FullName.StartsWith("media/") && !string.IsNullOrEmpty(e.Name))
            .ToList();

    /// <summary>
    /// Returns an error if the archive declares more decompressed data than allowed or
    /// contains a media entry that would be written outside the uploads directory.
    /// </summary>
    private string? CheckArchiveSafety(ZipArchive archive)
    {
        if (archive.Entries.Sum(e => e.Length) > _maxDecompressedBytes)
        {
            return "Package exceeds the maximum decompressed size";
        }

        var invalidEntry = GetMediaEntries(archive)
            .FirstOrDefault(e => ResolveMediaPath(e.FullName["media/".Length..]) == null);

        return invalidEntry != null ? $"Package contains an invalid media path: {invalidEntry.FullName}" : null;
    }

    /// <summary>
    /// Resolves a package-relative media path to an absolute path inside the uploads directory,
    /// or null if it would resolve anywhere else (e.g. via "../" segments or an absolute path).
    /// </summary>
    private string? ResolveMediaPath(string relativePath)
    {
        var uploadsRoot = Path.EndsInDirectorySeparator(_uploadsPath)
            ? _uploadsPath
            : _uploadsPath + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));

        return fullPath.StartsWith(uploadsRoot, StringComparison.Ordinal) ? fullPath : null;
    }

    /// <summary>
    /// Copies at most maxBytes from source to destination. Returns the number of bytes copied,
    /// or -1 if the source holds more than maxBytes.
    /// </summary>
    private static async Task<long> CopyWithLimitAsync(Stream source, Stream destination, long maxBytes)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return -1;
            }
            await destination.WriteAsync(buffer.AsMemory(0, read));
        }
        return total;
    }
}
