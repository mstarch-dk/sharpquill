using System.Text.Json;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Media;
using SharpQuill.Core.Articles;
using SharpQuill.Application.Common;
using Ganss.Xss;
using Markdig;

namespace SharpQuill.Application.Articles.Services;

public class LayoutComposer : ILayoutComposer
{
    private readonly IMediaRepository _mediaRepository;
    private readonly string _uploadsUrlBase;
    private readonly MarkdownPipeline _markdownPipeline;
    private readonly HtmlSanitizer _sanitizer;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LayoutComposer(IMediaRepository mediaRepository, string uploadsUrlBase)
    {
        _mediaRepository = mediaRepository;
        _uploadsUrlBase = uploadsUrlBase;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // Authors are not fully trusted: anything that reaches the browser as HTML passes through this allow-list
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "tel" })
        {
            _sanitizer.AllowedSchemes.Add(scheme);
        }

        // Inline images carry the size picked in the editor (TipTap SizedImage) as data-size; the public CSS turns
        // it into a column-relative width. Keep it, but only on <img> and only with a known value.
        _sanitizer.AllowedAttributes.Add("data-size");
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Dom.IElement element && element.HasAttribute("data-size") &&
                (element.LocalName != "img" || !ImageSizes.Contains(element.GetAttribute("data-size")!)))
            {
                element.RemoveAttribute("data-size");
            }
        };
    }

    private static readonly HashSet<string> ImageSizes = new() { "small", "medium", "large", "full" };

    private string RenderMarkdown(string markdown) => _sanitizer.Sanitize(Markdown.ToHtml(markdown, _markdownPipeline));

    public async Task<RenderedLayoutDto?> ComposeAsync(string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson))
            return null;

        var layout = JsonSerializer.Deserialize<ArticleLayout>(layoutJson, JsonOptions);
        if (layout?.Sections == null || layout.Sections.Count == 0)
            return null;

        // Collect all mediaIds recursively for batch fetching
        var mediaIds = new HashSet<int>();
        CollectMediaIds(layout.Sections, mediaIds);

        var mediaLookup = new Dictionary<int, string>();
        if (mediaIds.Count > 0)
        {
            var mediaItems = await _mediaRepository.GetByIdsAsync(mediaIds.ToList());
            foreach (var media in mediaItems)
            {
                mediaLookup[media.Id] = $"{_uploadsUrlBase}/{media.Path}";
            }
        }

        var renderedSections = RenderSections(layout.Sections, mediaLookup);
        return new RenderedLayoutDto(renderedSections);
    }

    private static void CollectMediaIds(List<LayoutSection> sections, HashSet<int> mediaIds)
    {
        foreach (var section in sections)
        {
            foreach (var block in section.Blocks)
            {
                var type = LayoutBlockTypes.Find(block.Type);
                if (type == null)
                    continue;
                if (type.UsesMedia && block.MediaId.HasValue)
                    mediaIds.Add(block.MediaId.Value);
                else if (type.NestsSections && block.Sections != null)
                    CollectMediaIds(block.Sections, mediaIds);
            }
        }
    }

    private List<RenderedSectionDto> RenderSections(List<LayoutSection> sections, Dictionary<int, string> mediaLookup)
    {
        var context = new RenderContext(this, mediaLookup);
        return sections.Select(section =>
        {
            var renderedBlocks = section.Blocks.Select(block => RenderBlock(block, context)).ToList();
            return new RenderedSectionDto(section.Type, renderedBlocks, section.Style);
        }).ToList();
    }

    // A stored block of a type this version does not know keeps only its type; the website skips it
    private static RenderedBlockDto RenderBlock(LayoutBlock block, ILayoutRenderContext context) =>
        LayoutBlockTypes.Find(block.Type)?.Render(block, context) ?? new RenderedBlockDto(block.Type);

    private string? RenderInlineMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var html = RenderMarkdown(text).Trim();
        // Strip single wrapping <p>...</p> for inline use
        if (html.StartsWith("<p>") && html.EndsWith("</p>") && html.IndexOf("<p>", 3) < 0)
            html = html[3..^4];
        return html;
    }

    private sealed class RenderContext(LayoutComposer composer, Dictionary<int, string> mediaLookup) : ILayoutRenderContext
    {
        public string Markdown(string markdown) => composer.RenderMarkdown(markdown);

        public string? InlineMarkdown(string? text) => composer.RenderInlineMarkdown(text);

        public string? MediaUrl(int? mediaId) =>
            mediaId.HasValue && mediaLookup.TryGetValue(mediaId.Value, out var url) ? url : null;

        public IReadOnlyList<RenderedSectionDto> RenderSections(List<LayoutSection> sections) =>
            composer.RenderSections(sections, mediaLookup);
    }

    public LayoutInputDto? ParseForAdmin(string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson))
            return null;

        var layout = JsonSerializer.Deserialize<ArticleLayout>(layoutJson, JsonOptions);
        if (layout?.Sections == null)
            return null;

        var sections = ParseSectionsForAdmin(layout.Sections);
        return new LayoutInputDto(sections);
    }

    private List<SectionInputDto> ParseSectionsForAdmin(List<LayoutSection> sections)
    {
        return sections.Select(s => new SectionInputDto(
            s.Type,
            s.Blocks.Select(b =>
            {
                if (b.Type == "container" && b.Sections != null)
                {
                    return new BlockInputDto(
                        b.Type,
                        Sections: ParseSectionsForAdmin(b.Sections)
                    );
                }
                var items = b.Items?.Select(i => new LayoutBlockItemDto(i.Icon, i.Number, i.Title, i.Description)).ToList();
                return new BlockInputDto(
                    b.Type,
                    Content: b.Content,
                    MediaId: b.MediaId,
                    Alt: b.Alt,
                    Caption: b.Caption,
                    Url: b.Url,
                    Title: b.Title,
                    Description: b.Description,
                    ImageUrl: b.ImageUrl,
                    SiteName: b.SiteName,
                    Language: b.Language,
                    TweetDate: b.TweetDate,
                    Heading: b.Heading,
                    ButtonText: b.ButtonText,
                    ButtonUrl: b.ButtonUrl,
                    Items: items,
                    ComponentName: b.ComponentName
                );
            }).ToList(),
            s.Style
        )).ToList();
    }

    public string? SerializeLayout(LayoutInputDto? layout)
    {
        if (layout == null || layout.Sections.Count == 0)
            return null;

        var articleLayout = new ArticleLayout
        {
            Version = 1,
            Sections = SerializeSections(layout.Sections)
        };

        return JsonSerializer.Serialize(articleLayout, JsonOptions);
    }

    private List<LayoutSection> SerializeSections(IReadOnlyList<SectionInputDto> sections)
    {
        return sections.Select(s => new LayoutSection
        {
            Type = s.Type,
            Style = s.Style,
            Blocks = s.Blocks.Select(b =>
            {
                if (b.Type == "container" && b.Sections != null)
                {
                    return new LayoutBlock
                    {
                        Type = b.Type,
                        Sections = SerializeSections(b.Sections)
                    };
                }
                var blockItems = b.Items?.Select(i => new LayoutBlockItem
                {
                    Icon = i.Icon,
                    Number = i.Number,
                    Title = i.Title,
                    Description = i.Description
                }).ToList();
                return new LayoutBlock
                {
                    Type = b.Type,
                    Content = b.Content,
                    MediaId = b.MediaId,
                    Alt = b.Alt,
                    Caption = b.Caption,
                    Url = b.Url,
                    Title = b.Title,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    SiteName = b.SiteName,
                    Language = b.Language,
                    TweetDate = b.TweetDate,
                    Heading = b.Heading,
                    ButtonText = b.ButtonText,
                    ButtonUrl = b.ButtonUrl,
                    Items = blockItems,
                    ComponentName = b.ComponentName
                };
            }).ToList()
        }).ToList();
    }

    public string? FlattenToMarkdown(LayoutInputDto? layout)
    {
        if (layout == null || layout.Sections.Count == 0)
            return null;

        var writer = new LayoutMarkdownWriter(FlattenSections);
        FlattenSections(layout.Sections, writer);

        var result = writer.Text.ToString();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static void FlattenSections(IReadOnlyList<SectionInputDto> sections, LayoutMarkdownWriter writer)
    {
        foreach (var section in sections)
        {
            foreach (var block in section.Blocks)
            {
                LayoutBlockTypes.Find(block.Type)?.Flatten?.Invoke(block, writer);
            }
        }
    }

    public static bool IsValidLayout(LayoutInputDto? layout)
    {
        if (layout == null)
            return true;

        if (layout.Sections.Count == 0)
            return false;

        return LayoutBlockTypes.AreValidSections(layout.Sections, 0);
    }
}
