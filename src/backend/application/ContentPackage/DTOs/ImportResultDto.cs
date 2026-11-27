namespace SharpQuill.Application.ContentPackage.DTOs;

public record ImportResultDto(
    int ArticlesImported,
    int PagesImported,
    int MediaImported,
    int CategoriesCreated,
    int TagsCreated,
    int SeriesCreated,
    int SettingsImported,
    int Skipped,
    IReadOnlyList<string> SkippedSlugs,
    IReadOnlyList<string> Errors
);
