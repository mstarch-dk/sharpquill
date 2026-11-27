namespace SharpQuill.Application.Navigations.DTOs;

public record NavigationDto(
    int Id,
    string Name,
    string Slug,
    IReadOnlyList<NavigationItemDto> Items
);

public record NavigationItemDto(
    int Id,
    string Label,
    string Url,
    int? ParentId,
    int Position,
    bool OpenInNewTab
);
