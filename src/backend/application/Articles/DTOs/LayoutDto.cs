using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.DTOs;

// --- Public API output (rendered HTML) ---

public record RenderedLayoutDto(
    IReadOnlyList<RenderedSectionDto> Sections
);

public record RenderedSectionDto(
    string Type,
    IReadOnlyList<RenderedBlockDto> Blocks,
    string? Style = null
);

public record RenderedBlockDto(
    string Type,
    string? Html = null,
    string? ImageUrl = null,
    string? Alt = null,
    string? Caption = null,
    IReadOnlyList<RenderedSectionDto>? Sections = null,
    string? Url = null,
    string? Title = null,
    string? Description = null,
    string? SiteName = null,
    string? Language = null,
    string? TweetDate = null,
    string? Heading = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    IReadOnlyList<RenderedBlockItemDto>? Items = null,
    string? DescriptionHtml = null,
    string? HeadingHtml = null,
    string? TitleHtml = null,
    string? CaptionHtml = null,
    string? ComponentName = null
);

public record RenderedBlockItemDto(
    string? Icon,
    string? Number,
    string? Title,
    string? Description,
    string? DescriptionHtml = null,
    string? TitleHtml = null
);

// --- Admin API input/output (raw markdown, mediaIds) ---

public record LayoutInputDto(
    IReadOnlyList<SectionInputDto> Sections
);

public record SectionInputDto(
    string Type,
    IReadOnlyList<BlockInputDto> Blocks,
    string? Style = null
);

public record LayoutBlockItemDto(
    string? Icon,
    string? Number,
    string? Title,
    string? Description
);

public record BlockInputDto(
    string Type,
    string? Content = null,
    int? MediaId = null,
    string? Alt = null,
    string? Caption = null,
    IReadOnlyList<SectionInputDto>? Sections = null,
    string? Url = null,
    string? Title = null,
    string? Description = null,
    string? ImageUrl = null,
    string? SiteName = null,
    string? Language = null,
    string? TweetDate = null,
    string? Heading = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    IReadOnlyList<LayoutBlockItemDto>? Items = null,
    string? ComponentName = null
);
