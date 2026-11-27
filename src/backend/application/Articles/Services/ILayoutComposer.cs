using SharpQuill.Application.Articles.DTOs;

namespace SharpQuill.Application.Articles.Services;

public interface ILayoutComposer
{
    Task<RenderedLayoutDto?> ComposeAsync(string? layoutJson);
    LayoutInputDto? ParseForAdmin(string? layoutJson);
    string? SerializeLayout(LayoutInputDto? layout);
    string? FlattenToMarkdown(LayoutInputDto? layout);
}
