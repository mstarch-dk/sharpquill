namespace SharpQuill.Application.Articles.DTOs;

public record ArticleSeriesInfoDto(
    int SeriesId,
    string SeriesName,
    string SeriesSlug,
    int CurrentOrder,
    int TotalInSeries,
    SeriesNavArticleDto? Previous,
    SeriesNavArticleDto? Next
);

public record SeriesNavArticleDto(
    int Id,
    string Title,
    string Slug
);

public record ArticleSeriesSummaryDto(
    int Id,
    string Name,
    string Slug
);
