using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.Series.Services;
using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Media;

namespace SharpQuill.Application.Articles.Services;

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _articleRepository;
    private readonly ISeriesService _seriesService;
    private readonly ILayoutComposer _layoutComposer;
    private readonly string _uploadsUrlBase;

    public ArticleService(
        IArticleRepository articleRepository,
        ISeriesService seriesService,
        ILayoutComposer layoutComposer,
        string uploadsUrlBase)
    {
        _articleRepository = articleRepository;
        _seriesService = seriesService;
        _layoutComposer = layoutComposer;
        _uploadsUrlBase = uploadsUrlBase;
    }

    public async Task<ArticleDto?> GetBySlugAsync(string slug, ContentType contentType = ContentType.Article)
    {
        var article = await _articleRepository.GetBySlugAsync(slug, contentType);
        if (article == null) return null;

        ArticleSeriesInfoDto? seriesInfo = null;
        if (article.SeriesId.HasValue && article.Series != null)
        {
            var info = await _seriesService.GetSeriesInfoForArticleAsync(article.Id, article.SeriesId.Value);
            if (info != null)
            {
                seriesInfo = info with
                {
                    SeriesName = article.Series.Name,
                    SeriesSlug = article.Series.Slug
                };
            }
        }

        return await MapToArticleDtoAsync(article, seriesInfo);
    }

    public async Task<PagedResultDto<ArticleListDto>> GetPublishedAsync(PaginationParams pagination)
    {
        var (articles, totalCount) = await _articleRepository.GetPublishedAsync(pagination);
        return CreatePagedResult(articles, totalCount, pagination);
    }

    public async Task<PagedResultDto<ArticleListDto>> GetByCategorySlugAsync(string categorySlug, PaginationParams pagination)
    {
        var (articles, totalCount) = await _articleRepository.GetByCategorySlugAsync(categorySlug, pagination);
        return CreatePagedResult(articles, totalCount, pagination);
    }

    public async Task<PagedResultDto<ArticleListDto>> GetByTagSlugAsync(string tagSlug, PaginationParams pagination)
    {
        var (articles, totalCount) = await _articleRepository.GetByTagSlugAsync(tagSlug, pagination);
        return CreatePagedResult(articles, totalCount, pagination);
    }

    public async Task<IReadOnlyList<ArticleListDto>> GetFeaturedAsync(int count = 5)
    {
        var articles = await _articleRepository.GetFeaturedAsync(count);
        return articles.Select(MapToArticleListDto).ToList();
    }

    public async Task<PagedResultDto<SearchResultDto>> SearchAsync(
        string? query, bool includeMembersOnly, PaginationParams pagination)
    {
        var normalized = NormalizeSearchQuery(query);
        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();

        var (hits, totalCount) = normalized.Length == 0
            ? ([], 0)
            : await _articleRepository.SearchPublishedAsync(normalized, includeMembersOnly, pagination);

        var data = hits
            .Select(hit => new SearchResultDto(MapToArticleListDto(hit.Article), ParseSnippet(hit.Snippet)))
            .ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<SearchResultDto>(data, new PaginationMeta(page, pageSize, totalCount, totalPages));
    }

    private static string NormalizeSearchQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;

        // Control characters carry no meaning in a query (and two of them are the snippet markers).
        var cleaned = new string(query.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        cleaned = string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Length > ArticleSearch.MaxQueryLength ? cleaned[..ArticleSearch.MaxQueryLength] : cleaned;
    }

    // Splits the marked-up ts_headline output into plain and matched runs.
    internal static IReadOnlyList<SearchSnippetPartDto> ParseSnippet(string snippet)
    {
        var parts = new List<SearchSnippetPartDto>();
        var position = 0;

        while (position < snippet.Length)
        {
            var start = snippet.IndexOf(ArticleSearch.MatchStart, position);
            if (start < 0)
            {
                AddPart(snippet[position..], match: false);
                break;
            }

            AddPart(snippet[position..start], match: false);

            var end = snippet.IndexOf(ArticleSearch.MatchEnd, start + 1);
            if (end < 0) end = snippet.Length;
            AddPart(snippet[(start + 1)..end], match: true);
            position = end + 1;
        }

        return parts;

        void AddPart(string text, bool match)
        {
            text = text.Replace(ArticleSearch.MatchStart.ToString(), string.Empty)
                .Replace(ArticleSearch.MatchEnd.ToString(), string.Empty);
            if (text.Length == 0) return;

            // ts_headline marks each word separately: join "a" + " " + "b" runs of the same kind.
            if (parts.Count > 0 && parts[^1].Match == match)
            {
                parts[^1] = parts[^1] with { Text = parts[^1].Text + text };
            }
            else
            {
                parts.Add(new SearchSnippetPartDto(text, match));
            }
        }
    }

    private PagedResultDto<ArticleListDto> CreatePagedResult(
        IEnumerable<Article> articles,
        int totalCount,
        PaginationParams pagination)
    {
        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = articles.Select(MapToArticleListDto).ToList();
        var meta = new PaginationMeta(page, pageSize, totalCount, totalPages);

        return new PagedResultDto<ArticleListDto>(data, meta);
    }

    private async Task<ArticleDto> MapToArticleDtoAsync(Article article, ArticleSeriesInfoDto? seriesInfo = null)
    {
        var renderedLayout = await _layoutComposer.ComposeAsync(article.Layout);

        return new ArticleDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Content,
            renderedLayout,
            article.Excerpt,
            article.MetaTitle,
            article.MetaDescription,
            GetFeaturedImageUrl(article),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            MapToAuthorDto(article.Author),
            MapToCategoryDto(article.Category),
            MapToTagSummaryDtos(article.ArticleTags),
            seriesInfo,
            article.CommentsEnabled,
            article.RequiresSignIn,
            article.PageStyle,
            FeaturedImageMediumUrl: GetFeaturedImageMediumUrl(article),
            FeaturedImageDisplayUrl: GetFeaturedImageDisplayUrl(article),
            FeaturedImageSize: FeaturedImageVariantResolver.NormalizeFeaturedImageSize(article.FeaturedImageSize),
            AuthorName: article.AuthorName
        );
    }

    private ArticleListDto MapToArticleListDto(Article article)
    {
        var seriesSummary = article.Series != null
            ? new ArticleSeriesSummaryDto(article.Series.Id, article.Series.Name, article.Series.Slug)
            : null;

        return new ArticleListDto(
            article.Id,
            article.Title,
            article.Slug,
            article.Excerpt,
            GetFeaturedImageUrl(article),
            article.PublishedAt,
            MapToAuthorDto(article.Author),
            MapToCategoryDto(article.Category),
            MapToTagSummaryDtos(article.ArticleTags),
            seriesSummary,
            article.RequiresSignIn,
            FeaturedImageThumbnailUrl: GetFeaturedImageThumbnailUrl(article),
            AuthorName: article.AuthorName
        );
    }

    private string? GetFeaturedImageUrl(Article article)
    {
        return article.FeaturedImage != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.Path}"
            : null;
    }

    private string? GetFeaturedImageThumbnailUrl(Article article)
    {
        return article.FeaturedImage?.ThumbnailPath != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.ThumbnailPath}"
            : null;
    }

    private string? GetFeaturedImageMediumUrl(Article article)
    {
        return article.FeaturedImage?.MediumPath != null
            ? $"{_uploadsUrlBase}/{article.FeaturedImage.MediumPath}"
            : null;
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

    private static AuthorDto? MapToAuthorDto(Author? author)
    {
        return author != null ? new AuthorDto(author.Id, author.Name, author.AvatarUrl) : null;
    }

    private static CategoryDto? MapToCategoryDto(Category? category)
    {
        return category == null
            ? null
            : new CategoryDto(category.Id, category.Name, category.Slug, category.Description, 0);
    }

    private static IReadOnlyList<TagSummaryDto> MapToTagSummaryDtos(ICollection<ArticleTag> articleTags)
    {
        return articleTags
            .Select(at => new TagSummaryDto(at.Tag.Id, at.Tag.Name, at.Tag.Slug))
            .ToList();
    }
}
