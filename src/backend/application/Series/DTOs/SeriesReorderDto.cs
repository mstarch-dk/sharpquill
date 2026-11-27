namespace SharpQuill.Application.Series.DTOs;

public record SeriesReorderDto(
    IReadOnlyList<int> ArticleIds
);
