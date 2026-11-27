namespace SharpQuill.Application.ContentPackage.DTOs;

public record ExportRequestDto(
    string? ContentType,
    List<int>? ArticleIds,
    bool IncludeTags = false,
    bool IncludeCategories = false,
    bool IncludeSettings = false
);
