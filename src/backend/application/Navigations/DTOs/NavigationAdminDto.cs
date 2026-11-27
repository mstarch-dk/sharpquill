using SharpQuill.Core.Navigations;

namespace SharpQuill.Application.Navigations.DTOs;

public record NavigationAdminDto(
    int Id,
    string Name,
    string Slug,
    IReadOnlyList<NavigationItemAdminDto> Items
);

public record NavigationItemAdminDto(
    int Id,
    string Label,
    NavigationItemType ItemType,
    int? ArticleId,
    string? PageTitle,
    string? PageSlug,
    string? SystemRoute,
    string? Url,
    int? ParentId,
    int Position,
    bool OpenInNewTab
);
