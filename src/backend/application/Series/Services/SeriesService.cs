using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Series.DTOs;
using SharpQuill.Application.Series.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Series;

namespace SharpQuill.Application.Series.Services;

public class SeriesService : ISeriesService
{
    private readonly ISeriesRepository _seriesRepository;

    public SeriesService(ISeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }

    public async Task<IReadOnlyList<SeriesDto>> GetAllAsync()
    {
        var seriesList = await _seriesRepository.GetAllWithArticleCountAsync();
        return seriesList.Select(s =>
        {
            var articleCount = (s as SeriesWithCount)?.ArticleCount ?? 0;
            return new SeriesDto(s.Id, s.Name, s.Slug, s.Description, articleCount, s.ThemeSlug);
        }).ToList();
    }

    public async Task<SeriesDetailDto?> GetBySlugAsync(string slug)
    {
        var series = await _seriesRepository.GetBySlugAsync(slug);
        if (series == null) return null;

        var articles = await _seriesRepository.GetArticlesBySeriesIdAsync(series.Id);
        var articleDtos = articles.Select(a => new SeriesArticleDto(
            a.Id,
            a.Title,
            a.Slug,
            a.Excerpt,
            a.PublishedAt,
            a.SeriesOrder ?? 0
        )).ToList();

        return new SeriesDetailDto(
            series.Id,
            series.Name,
            series.Slug,
            series.Description,
            articleDtos,
            series.ThemeSlug
        );
    }

    public async Task<ArticleSeriesInfoDto?> GetSeriesInfoForArticleAsync(int articleId, int seriesId)
    {
        var articles = (await _seriesRepository.GetArticlesBySeriesIdAsync(seriesId)).ToList();
        if (articles.Count == 0) return null;

        var currentIndex = articles.FindIndex(a => a.Id == articleId);
        if (currentIndex < 0) return null;

        SeriesNavArticleDto? previous = currentIndex > 0
            ? new SeriesNavArticleDto(articles[currentIndex - 1].Id, articles[currentIndex - 1].Title, articles[currentIndex - 1].Slug)
            : null;

        SeriesNavArticleDto? next = currentIndex < articles.Count - 1
            ? new SeriesNavArticleDto(articles[currentIndex + 1].Id, articles[currentIndex + 1].Title, articles[currentIndex + 1].Slug)
            : null;

        // Return without series name/slug - caller will provide those from the article's Series entity
        return new ArticleSeriesInfoDto(
            seriesId,
            string.Empty,
            string.Empty,
            currentIndex + 1,
            articles.Count,
            previous,
            next
        );
    }
}
