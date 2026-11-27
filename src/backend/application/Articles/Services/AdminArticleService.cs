using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Services;

public class AdminArticleService : IAdminArticleService
{
    private readonly IAdminArticleRepository _articleRepository;
    private readonly IArticleContentRepository _contentRepository;
    private readonly ILayoutComposer _layoutComposer;
    private readonly string _uploadsUrlBase;

    public AdminArticleService(
        IAdminArticleRepository articleRepository,
        IArticleContentRepository contentRepository,
        ILayoutComposer layoutComposer,
        string uploadsUrlBase)
    {
        _articleRepository = articleRepository;
        _contentRepository = contentRepository;
        _layoutComposer = layoutComposer;
        _uploadsUrlBase = uploadsUrlBase;
    }

    public async Task<ArticleAdminDto?> GetByIdAsync(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        return article == null ? null : MapToArticleAdminDto(article);
    }

    public async Task<PagedResultDto<ArticleAdminDto>> GetAllAsync(PaginationParams pagination, ContentType? contentType = null, ArticleAdminFilter? filter = null)
    {
        var (articles, totalCount) = await _articleRepository.GetAllAsync(pagination, contentType, filter);

        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = articles.Select(MapToArticleAdminDto).ToList();
        var meta = new PaginationMeta(page, pageSize, totalCount, totalPages);

        return new PagedResultDto<ArticleAdminDto>(data, meta);
    }

    public async Task<(ArticleAdminDto? Article, string? Error)> CreateAsync(ArticleCreateDto dto, int? authorId)
    {
        // Check for slug uniqueness
        if (await _articleRepository.SlugExistsAsync(dto.Slug))
        {
            return (null, "An item with this slug already exists");
        }

        // Serialize layout and flatten to markdown for content field
        var layoutJson = _layoutComposer.SerializeLayout(dto.Layout);
        var content = dto.Layout != null
            ? _layoutComposer.FlattenToMarkdown(dto.Layout)
            : dto.Content;

        var article = new Article
        {
            Title = dto.Title,
            Slug = dto.Slug,
            CategoryId = dto.CategoryId,
            FeaturedImageId = dto.FeaturedImageId,
            Status = dto.Status,
            ContentType = dto.ContentType,
            CommentsEnabled = dto.CommentsEnabled,
            RequiresSignIn = dto.RequiresSignIn,
            AuthorId = authorId,
            AuthorName = dto.AuthorName,
            CreatedAt = DateTime.UtcNow
        };

        // Set published date: use provided value if given, otherwise auto-set when Published
        if (dto.PublishedAt.HasValue)
        {
            article.PublishedAt = dto.PublishedAt.Value;
        }
        else if (dto.Status == ArticleStatus.Published)
        {
            article.PublishedAt = DateTime.UtcNow;
        }

        var created = await _articleRepository.CreateAsync(article);

        // Create version 1 with content
        var version = new ArticleContent
        {
            ArticleId = created.Id,
            Content = content,
            Layout = layoutJson,
            Excerpt = dto.Excerpt,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            PageStyle = dto.PageStyle,
            FeaturedImageSize = FeaturedImageVariantResolver.NormalizeFeaturedImageSize(dto.FeaturedImageSize),
            VersionNumber = 1,
            CreatedAt = DateTime.UtcNow
        };
        var createdVersion = await _contentRepository.CreateVersionAsync(version);

        // Auto-set published_version_id if status is Published
        if (dto.Status == ArticleStatus.Published)
        {
            await _articleRepository.SetPublishedVersionAsync(created.Id, createdVersion.Id);
        }

        // Update tags if provided (articles only)
        if (dto.TagIds != null && dto.TagIds.Count > 0)
        {
            await _articleRepository.UpdateTagsAsync(created.Id, dto.TagIds);
        }

        // Fetch the complete article with relationships
        var result = await _articleRepository.GetByIdAsync(created.Id);
        return (result != null ? MapToArticleAdminDto(result) : null, null);
    }

    public async Task<(ArticleAdminDto? Article, string? Error)> UpdateAsync(int id, ArticleUpdateDto dto)
    {
        var existing = await _articleRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (null, "Item not found");
        }

        // Check for slug uniqueness (excluding current article)
        if (await _articleRepository.SlugExistsAsync(dto.Slug, id))
        {
            return (null, "An item with this slug already exists");
        }

        // Serialize layout and flatten to markdown for content field
        var layoutJson = _layoutComposer.SerializeLayout(dto.Layout);
        var content = dto.Layout != null
            ? _layoutComposer.FlattenToMarkdown(dto.Layout)
            : dto.Content;

        // Check if content fields changed - create new version only if they differ
        var latestVersion = await _contentRepository.GetLatestVersionAsync(id);
        var contentChanged = latestVersion == null ||
            latestVersion.Content != content ||
            latestVersion.Layout != layoutJson ||
            latestVersion.Excerpt != dto.Excerpt ||
            latestVersion.MetaTitle != dto.MetaTitle ||
            latestVersion.MetaDescription != dto.MetaDescription ||
            latestVersion.PageStyle != dto.PageStyle ||
            FeaturedImageVariantResolver.NormalizeFeaturedImageSize(latestVersion.FeaturedImageSize) != FeaturedImageVariantResolver.NormalizeFeaturedImageSize(dto.FeaturedImageSize);

        int? newVersionId = null;
        if (contentChanged)
        {
            var nextVersionNumber = await _contentRepository.GetNextVersionNumberAsync(id);
            var newVersion = new ArticleContent
            {
                ArticleId = id,
                Content = content,
                Layout = layoutJson,
                Excerpt = dto.Excerpt,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                PageStyle = dto.PageStyle,
                FeaturedImageSize = FeaturedImageVariantResolver.NormalizeFeaturedImageSize(dto.FeaturedImageSize),
                VersionNumber = nextVersionNumber,
                CreatedAt = DateTime.UtcNow
            };
            var created = await _contentRepository.CreateVersionAsync(newVersion);
            newVersionId = created.Id;
        }

        existing.Title = dto.Title;
        existing.Slug = dto.Slug;
        existing.CategoryId = dto.CategoryId;
        existing.FeaturedImageId = dto.FeaturedImageId;
        existing.Status = dto.Status;
        existing.CommentsEnabled = dto.CommentsEnabled;
        existing.RequiresSignIn = dto.RequiresSignIn;
        existing.AuthorName = dto.AuthorName;
        existing.UpdatedAt = DateTime.UtcNow;

        // If transitioning to Published, set publish date: provided value takes precedence
        if (dto.Status == ArticleStatus.Published)
        {
            if (dto.PublishedAt.HasValue)
            {
                existing.PublishedAt = dto.PublishedAt.Value;
            }
            else if (!existing.PublishedAt.HasValue)
            {
                existing.PublishedAt = DateTime.UtcNow;
            }
            // If content changed, update published version to the new version
            if (newVersionId.HasValue)
            {
                existing.PublishedVersionId = newVersionId.Value;
            }
            // If no published version set yet, use latest
            else if (!existing.PublishedVersionId.HasValue && latestVersion != null)
            {
                existing.PublishedVersionId = latestVersion.Id;
            }
        }

        await _articleRepository.UpdateAsync(existing);

        // Update tags
        await _articleRepository.UpdateTagsAsync(id, dto.TagIds ?? Array.Empty<int>());

        // Fetch the updated article with relationships
        var result = await _articleRepository.GetByIdAsync(id);
        return (result != null ? MapToArticleAdminDto(result) : null, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _articleRepository.DeleteAsync(id);
    }

    public async Task<IReadOnlyList<ArticleVersionDto>?> GetVersionsAsync(int articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
            return null;

        var versions = await _contentRepository.GetVersionsByArticleIdAsync(articleId);
        return versions.Select(v => new ArticleVersionDto(v.Id, v.VersionNumber, v.CreatedAt)).ToList();
    }

    public async Task<ArticleVersionDetailDto?> GetVersionAsync(int articleId, int versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
            return null;

        var version = await _contentRepository.GetByIdAsync(versionId);
        if (version == null || version.ArticleId != articleId)
            return null;

        return new ArticleVersionDetailDto(
            version.Id,
            version.VersionNumber,
            version.Content,
            _layoutComposer.ParseForAdmin(version.Layout),
            version.Excerpt,
            version.MetaTitle,
            version.MetaDescription,
            version.CreatedAt,
            version.PageStyle,
            version.FeaturedImageSize
        );
    }

    public async Task<(ArticleAdminDto? Article, string? Error)> PublishVersionAsync(int articleId, int versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
            return (null, "Item not found");

        var version = await _contentRepository.GetByIdAsync(versionId);
        if (version == null || version.ArticleId != articleId)
            return (null, "Version not found");

        await _articleRepository.SetPublishedVersionAsync(articleId, versionId);

        var result = await _articleRepository.GetByIdAsync(articleId);
        return (result != null ? MapToArticleAdminDto(result) : null, null);
    }

    public async Task<ArticleDto?> GetPreviewAsync(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null) return null;

        var renderedLayout = await _layoutComposer.ComposeAsync(article.Layout);

        var featuredImageUrl = article.FeaturedImage != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.Path}"
            : null;

        var featuredImageMediumUrl = article.FeaturedImage?.MediumPath != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.MediumPath}"
            : null;

        var featuredImageDisplayUrl = GetFeaturedImageDisplayUrl(article);

        var authorDto = article.Author != null
            ? new AuthorDto(article.Author.Id, article.Author.Name, article.Author.AvatarUrl)
            : null;

        var categoryDto = article.Category != null
            ? new CategoryDto(article.Category.Id, article.Category.Name, article.Category.Slug, article.Category.Description, 0)
            : null;

        var tags = article.ArticleTags
            .Select(at => new TagSummaryDto(at.Tag.Id, at.Tag.Name, at.Tag.Slug))
            .ToList();

        return new ArticleDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Content,
            renderedLayout,
            article.Excerpt,
            article.MetaTitle,
            article.MetaDescription,
            featuredImageUrl,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            authorDto,
            categoryDto,
            tags,
            null,
            article.CommentsEnabled,
            article.RequiresSignIn,
            article.PageStyle,
            FeaturedImageMediumUrl: featuredImageMediumUrl,
            FeaturedImageDisplayUrl: featuredImageDisplayUrl,
            FeaturedImageSize: FeaturedImageVariantResolver.NormalizeFeaturedImageSize(article.FeaturedImageSize),
            AuthorName: article.AuthorName
        );
    }

    private ArticleAdminDto MapToArticleAdminDto(Article article)
    {
        var seriesSummary = article.Series != null
            ? new ArticleSeriesSummaryDto(article.Series.Id, article.Series.Name, article.Series.Slug)
            : null;

        var featuredImageUrl = article.FeaturedImage != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.Path}"
            : null;

        var authorDto = article.Author != null
            ? new AuthorDto(article.Author.Id, article.Author.Name, article.Author.AvatarUrl)
            : null;

        return new ArticleAdminDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Content,
            _layoutComposer.ParseForAdmin(article.Layout),
            article.Excerpt,
            article.MetaTitle,
            article.MetaDescription,
            article.FeaturedImageId,
            featuredImageUrl,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            article.Status,
            article.ContentType,
            authorDto,
            article.Category != null
                ? new CategoryDto(article.Category.Id, article.Category.Name, article.Category.Slug, article.Category.Description, 0)
                : null,
            article.ArticleTags.Select(at => new TagSummaryDto(at.Tag.Id, at.Tag.Name, at.Tag.Slug)).ToList(),
            seriesSummary,
            article.SeriesOrder,
            article.CurrentVersionNumber,
            article.TotalVersions,
            article.PublishedVersionId,
            article.CommentsEnabled,
            article.RequiresSignIn,
            article.PageStyle,
            FeaturedImageSize: FeaturedImageVariantResolver.NormalizeFeaturedImageSize(article.FeaturedImageSize),
            FeaturedImageDisplayUrl: GetFeaturedImageDisplayUrl(article),
            AuthorName: article.AuthorName
        );
    }

    private string? GetFeaturedImageDisplayUrl(Article article)
    {
        if (article.FeaturedImage == null)
            return null;

        var originalUrl = $"{_uploadsUrlBase}/{article.FeaturedImage.Path}";
        var thumbnailUrl = article.FeaturedImage.ThumbnailPath != null ? $"{_uploadsUrlBase}/{article.FeaturedImage.ThumbnailPath}" : null;
        var mediumUrl = article.FeaturedImage.MediumPath != null ? $"{_uploadsUrlBase}/{article.FeaturedImage.MediumPath}" : null;

        return FeaturedImageVariantResolver.ResolveDisplayUrl(
            article.FeaturedImageSize ?? "medium",
            originalUrl,
            thumbnailUrl,
            mediumUrl
        );
    }
}
