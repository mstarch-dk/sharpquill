using SharpQuill.Application.Series.DTOs;
using SharpQuill.Application.Series.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Series;

namespace SharpQuill.Application.Series.Services;

public class AdminSeriesService : IAdminSeriesService
{
    private readonly IAdminSeriesRepository _seriesRepository;
    private readonly IAdminArticleRepository _articleRepository;

    public AdminSeriesService(IAdminSeriesRepository seriesRepository, IAdminArticleRepository articleRepository)
    {
        _seriesRepository = seriesRepository;
        _articleRepository = articleRepository;
    }

    public async Task<SeriesAdminDetailDto?> GetByIdAsync(int id)
    {
        var series = await _seriesRepository.GetByIdAsync(id);
        if (series == null) return null;

        var articles = await _seriesRepository.GetArticlesInSeriesAsync(id);
        var articleCount = await _seriesRepository.GetArticleCountAsync(id);

        var articleDtos = articles.Select(a => new SeriesArticleAdminDto(
            a.Id,
            a.Title,
            a.Slug,
            a.Excerpt,
            a.PublishedAt,
            a.Status,
            a.Author?.Name ?? string.Empty,
            a.SeriesOrder ?? 0
        )).ToList();

        return new SeriesAdminDetailDto(
            series.Id,
            series.Name,
            series.Slug,
            series.Description,
            series.CreatedAt,
            series.UpdatedAt,
            articleCount,
            articleDtos,
            series.ThemeSlug
        );
    }

    public async Task<IReadOnlyList<SeriesDto>> GetAllAsync()
    {
        var seriesList = await _seriesRepository.GetAllAsync();
        return seriesList.Select(s =>
        {
            var articleCount = (s as SeriesWithCount)?.ArticleCount ?? 0;
            return new SeriesDto(s.Id, s.Name, s.Slug, s.Description, articleCount, s.ThemeSlug);
        }).ToList();
    }

    public async Task<(SeriesDto? Series, string? Error)> CreateAsync(SeriesCreateDto dto)
    {
        if (await _seriesRepository.SlugExistsAsync(dto.Slug))
        {
            return (null, "A series with this slug already exists");
        }

        var series = new ArticleSeries
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            ThemeSlug = dto.ThemeSlug,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _seriesRepository.CreateAsync(series);
        return (new SeriesDto(created.Id, created.Name, created.Slug, created.Description, 0, created.ThemeSlug), null);
    }

    public async Task<(SeriesDto? Series, string? Error)> UpdateAsync(int id, SeriesUpdateDto dto)
    {
        var existing = await _seriesRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (null, "Series not found");
        }

        if (await _seriesRepository.SlugExistsAsync(dto.Slug, id))
        {
            return (null, "A series with this slug already exists");
        }

        existing.Name = dto.Name;
        existing.Slug = dto.Slug;
        existing.Description = dto.Description;
        existing.ThemeSlug = dto.ThemeSlug;
        existing.UpdatedAt = DateTime.UtcNow;

        await _seriesRepository.UpdateAsync(existing);

        var articleCount = await _seriesRepository.GetArticleCountAsync(id);
        return (new SeriesDto(existing.Id, existing.Name, existing.Slug, existing.Description, articleCount, existing.ThemeSlug), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var existing = await _seriesRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (false, "Series not found");
        }

        var deleted = await _seriesRepository.DeleteAsync(id);
        return (deleted, null);
    }

    public async Task<(bool Success, string? Error)> AddArticleAsync(int seriesId, SeriesAddArticleDto dto)
    {
        var series = await _seriesRepository.GetByIdAsync(seriesId);
        if (series == null)
        {
            return (false, "Series not found");
        }

        var article = await _articleRepository.GetByIdAsync(dto.ArticleId);
        if (article == null)
        {
            return (false, "Article not found");
        }

        if (article.SeriesId.HasValue && article.SeriesId.Value != seriesId)
        {
            return (false, "Article already belongs to another series");
        }

        // Determine position
        var existingArticles = (await _seriesRepository.GetArticlesInSeriesAsync(seriesId)).ToList();
        int order;

        if (dto.Position.HasValue && dto.Position.Value >= 1 && dto.Position.Value <= existingArticles.Count + 1)
        {
            order = dto.Position.Value;
            // Shift existing articles at or after this position
            var toShift = existingArticles.Where(a => a.SeriesOrder >= order).OrderByDescending(a => a.SeriesOrder);
            foreach (var a in toShift)
            {
                await _seriesRepository.SetArticleSeriesAsync(a.Id, seriesId, (a.SeriesOrder ?? 0) + 1);
            }
        }
        else
        {
            order = existingArticles.Count + 1;
        }

        await _seriesRepository.SetArticleSeriesAsync(dto.ArticleId, seriesId, order);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveArticleAsync(int seriesId, int articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return (false, "Article not found");
        }

        if (article.SeriesId != seriesId)
        {
            return (false, "Article does not belong to this series");
        }

        var removedOrder = article.SeriesOrder ?? 0;
        await _seriesRepository.RemoveArticleFromSeriesAsync(articleId);

        // Re-compact ordering
        var remaining = (await _seriesRepository.GetArticlesInSeriesAsync(seriesId))
            .OrderBy(a => a.SeriesOrder)
            .ToList();

        var reorderedIds = remaining.Select(a => a.Id).ToList();
        if (reorderedIds.Count > 0)
        {
            await _seriesRepository.ReorderArticlesAsync(seriesId, reorderedIds);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReorderArticlesAsync(int seriesId, SeriesReorderDto dto)
    {
        var series = await _seriesRepository.GetByIdAsync(seriesId);
        if (series == null)
        {
            return (false, "Series not found");
        }

        var existingArticles = (await _seriesRepository.GetArticlesInSeriesAsync(seriesId)).ToList();
        var existingIds = existingArticles.Select(a => a.Id).ToHashSet();

        // Validate all IDs belong to this series
        foreach (var id in dto.ArticleIds)
        {
            if (!existingIds.Contains(id))
            {
                return (false, $"Article {id} does not belong to this series");
            }
        }

        await _seriesRepository.ReorderArticlesAsync(seriesId, dto.ArticleIds);
        return (true, null);
    }
}
