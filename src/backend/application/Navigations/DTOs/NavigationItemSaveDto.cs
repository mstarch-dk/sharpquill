using SharpQuill.Core.Navigations;

namespace SharpQuill.Application.Navigations.DTOs;

public record NavigationItemSaveDto(
    string Label,
    NavigationItemType ItemType,
    int? ArticleId,
    string? SystemRoute,
    string? Url,
    int? ParentId,
    int Position,
    bool OpenInNewTab
);

public record NavigationItemsBulkSaveDto(
    IReadOnlyList<NavigationItemSaveDto> Items
);
