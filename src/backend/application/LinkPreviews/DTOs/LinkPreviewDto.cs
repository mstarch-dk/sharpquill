namespace SharpQuill.Application.LinkPreviews.DTOs;

public record LinkPreviewDto(
    string? Title,
    string? Description,
    string? ImageUrl,
    string? SiteName
);
