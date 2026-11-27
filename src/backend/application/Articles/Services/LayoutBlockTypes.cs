using System.Text;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Common;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Services;

/// <summary>
/// What a layout block type needs from the composer while rendering: sanitised Markdown, media URLs and nested sections.
/// </summary>
public interface ILayoutRenderContext
{
    /// <summary>Markdown rendered to sanitised HTML.</summary>
    string Markdown(string markdown);

    /// <summary>Like <see cref="Markdown"/>, without the wrapping paragraph of a single-paragraph text; null when blank.</summary>
    string? InlineMarkdown(string? text);

    /// <summary>Public URL of a media item, or null when it is missing or does not exist.</summary>
    string? MediaUrl(int? mediaId);

    IReadOnlyList<RenderedSectionDto> RenderSections(List<LayoutSection> sections);
}

/// <summary>Plain-Markdown output of a layout (article content for search and feeds).</summary>
public sealed class LayoutMarkdownWriter(Action<IReadOnlyList<SectionInputDto>, LayoutMarkdownWriter> writeSections)
{
    public StringBuilder Text { get; } = new();

    /// <summary>Starts a new line unless nothing has been written yet.</summary>
    public void Separate()
    {
        if (Text.Length > 0)
            Text.AppendLine();
    }

    /// <summary>Writes nested sections (container blocks).</summary>
    public void Sections(IReadOnlyList<SectionInputDto> sections) => writeSections(sections, this);
}

/// <summary>One block type of the layout editor: how the API validates, renders and flattens it.</summary>
/// <param name="Name">Stored in the layout JSON as the block's type.</param>
/// <param name="IsValid">Type-specific checks of an incoming block; <c>depth</c> is the nesting level of its section.</param>
/// <param name="Render">Turns the stored block into what the public API (and the admin preview) returns.</param>
/// <param name="Flatten">Writes the block as plain Markdown; null when it has no text worth indexing.</param>
/// <param name="UsesMedia">The block's <c>MediaId</c> is resolved to a URL before rendering.</param>
/// <param name="NestsSections">The block holds nested sections (rendering collects their media too).</param>
public sealed record LayoutBlockType(
    string Name,
    Func<BlockInputDto, int, bool> IsValid,
    Func<LayoutBlock, ILayoutRenderContext, RenderedBlockDto> Render,
    Action<BlockInputDto, LayoutMarkdownWriter>? Flatten = null,
    bool UsesMedia = false,
    bool NestsSections = false);

/// <summary>
/// The layout block types and embeddable components the API accepts. The frontend registry in the shared Nuxt layer
/// (src/frontend/layers/shared/app/blocks/&lt;type&gt;/ and embeds/&lt;name&gt;/) must list the same names; the
/// LayoutBlockRegistryTests compare them. Adding a block: docs/frontend-uis.md, "Adding a block".
/// </summary>
public static class LayoutBlockTypes
{
    public const int MaxNestingDepth = 3;

    public static readonly IReadOnlySet<string> SectionTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "full", "two-column", "three-column"
    };

    /// <summary>Values of a component block's componentName (the shared layer's embeds/&lt;name&gt;/ folders).</summary>
    public static readonly IReadOnlySet<string> ComponentNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "ContactForm"
    };

    private static readonly LayoutBlockType[] Types =
    [
        new("text",
            IsValid: (b, _) => !HasSections(b) && !string.IsNullOrWhiteSpace(b.Content),
            Render: (b, ctx) => new RenderedBlockDto("text", Html: ctx.Markdown(b.Content ?? "")),
            Flatten: (b, md) =>
            {
                if (string.IsNullOrWhiteSpace(b.Content)) return;
                md.Separate();
                md.Text.Append(b.Content);
            }),

        new("image",
            IsValid: (b, _) => !HasSections(b) && b.MediaId.HasValue,
            Render: (b, ctx) => new RenderedBlockDto("image", ImageUrl: ctx.MediaUrl(b.MediaId), Alt: b.Alt,
                Caption: b.Caption, CaptionHtml: ctx.InlineMarkdown(b.Caption)),
            UsesMedia: true),

        new("code",
            IsValid: (b, _) => !string.IsNullOrWhiteSpace(b.Content) && !b.MediaId.HasValue && !HasSections(b),
            Render: (b, _) =>
            {
                var escaped = System.Net.WebUtility.HtmlEncode(b.Content ?? "");
                var lang = b.Language ?? "";
                var langClass = string.IsNullOrWhiteSpace(lang) ? "" : $" class=\"language-{lang}\"";
                return new RenderedBlockDto("code", Html: $"<pre><code{langClass}>{escaped}</code></pre>", Language: b.Language);
            },
            Flatten: (b, md) =>
            {
                if (string.IsNullOrWhiteSpace(b.Content)) return;
                md.Separate();
                md.Text.AppendLine($"```{b.Language ?? ""}");
                md.Text.Append(b.Content);
                md.Text.AppendLine();
                md.Text.Append("```");
            }),

        new("link",
            IsValid: (b, _) => !string.IsNullOrWhiteSpace(b.Url) && !b.MediaId.HasValue && !HasSections(b),
            Render: (b, _) => new RenderedBlockDto("link",
                Url: UrlSafety.OrNull(b.Url),
                Title: b.Title,
                Description: b.Description,
                ImageUrl: UrlSafety.OrNull(b.ImageUrl),
                SiteName: b.SiteName),
            Flatten: (b, md) =>
            {
                if (string.IsNullOrWhiteSpace(b.Url)) return;
                md.Separate();
                md.Text.Append($"[{b.Title ?? b.Url}]({b.Url})");
            }),

        new("tweet",
            IsValid: (b, _) => !string.IsNullOrWhiteSpace(b.Url)
                && (b.Url.Contains("twitter.com") || b.Url.Contains("x.com"))
                && !b.MediaId.HasValue && !HasSections(b),
            Render: (b, _) => new RenderedBlockDto("tweet",
                Url: UrlSafety.OrNull(b.Url),
                Title: b.Title,
                Description: b.Description,
                SiteName: b.SiteName,
                ImageUrl: UrlSafety.OrNull(b.ImageUrl),
                TweetDate: b.TweetDate),
            Flatten: (b, md) =>
            {
                if (string.IsNullOrWhiteSpace(b.Url)) return;
                md.Separate();
                md.Text.Append($"[Tweet]({b.Url})");
            }),

        new("container",
            IsValid: (b, depth) => b.Sections is { Count: > 0 }
                && string.IsNullOrWhiteSpace(b.Content) && !b.MediaId.HasValue
                && AreValidSections(b.Sections, depth + 1),
            Render: (b, ctx) => b.Sections != null
                ? new RenderedBlockDto("container", Sections: ctx.RenderSections(b.Sections))
                : new RenderedBlockDto("container"),
            Flatten: (b, md) =>
            {
                if (b.Sections != null) md.Sections(b.Sections);
            },
            NestsSections: true),

        new("cards",
            IsValid: (b, _) => b.Items is { Count: > 0 } && !HasSections(b),
            Render: (b, ctx) => new RenderedBlockDto("cards",
                Html: b.Content != null ? ctx.Markdown(b.Content) : null,
                Heading: b.Heading,
                Items: RenderItems(b.Items, ctx),
                HeadingHtml: ctx.InlineMarkdown(b.Heading)),
            Flatten: FlattenItems),

        new("steps",
            IsValid: (b, _) => b.Items is { Count: > 0 } && !HasSections(b),
            Render: (b, ctx) => new RenderedBlockDto("steps",
                Heading: b.Heading,
                Items: RenderItems(b.Items, ctx),
                HeadingHtml: ctx.InlineMarkdown(b.Heading)),
            Flatten: FlattenItems),

        new("cta",
            IsValid: (b, _) => !string.IsNullOrWhiteSpace(b.Title) && !HasSections(b),
            Render: (b, ctx) => new RenderedBlockDto("cta",
                Title: b.Title,
                Description: b.Description,
                ButtonText: b.ButtonText,
                ButtonUrl: UrlSafety.OrNull(b.ButtonUrl),
                DescriptionHtml: b.Description != null ? ctx.Markdown(b.Description) : null,
                TitleHtml: ctx.InlineMarkdown(b.Title)),
            Flatten: (b, md) =>
            {
                if (!string.IsNullOrWhiteSpace(b.Title))
                {
                    md.Separate();
                    md.Text.AppendLine($"## {b.Title}");
                }
                if (!string.IsNullOrWhiteSpace(b.Description))
                    md.Text.Append(b.Description);
            }),

        new("component",
            IsValid: (b, _) => !string.IsNullOrWhiteSpace(b.ComponentName) && ComponentNames.Contains(b.ComponentName)
                && !HasSections(b),
            Render: (b, _) => new RenderedBlockDto("component", ComponentName: b.ComponentName)),
    ];

    private static readonly Dictionary<string, LayoutBlockType> ByName = Types.ToDictionary(t => t.Name, StringComparer.Ordinal);

    public static IReadOnlyCollection<string> Names => ByName.Keys;

    /// <summary>The block type with this name, or null for an unknown one.</summary>
    public static LayoutBlockType? Find(string? name) =>
        name != null && ByName.TryGetValue(name, out var type) ? type : null;

    /// <summary>
    /// Checks sections and their blocks at nesting level <paramref name="depth"/>: known section and block types, safe
    /// URLs, the type's own rules, and at most <see cref="MaxNestingDepth"/> levels.
    /// </summary>
    public static bool AreValidSections(IReadOnlyList<SectionInputDto> sections, int depth)
    {
        if (depth >= MaxNestingDepth)
            return false;

        foreach (var section in sections)
        {
            if (!SectionTypes.Contains(section.Type) || section.Blocks.Count == 0)
                return false;

            foreach (var block in section.Blocks)
            {
                var type = Find(block.Type);
                if (type == null)
                    return false;

                // Links/images must not carry script-capable URLs (javascript:, data:, ...)
                if (!UrlSafety.IsSafe(block.Url) || !UrlSafety.IsSafe(block.ImageUrl) || !UrlSafety.IsSafe(block.ButtonUrl))
                    return false;

                if (!type.IsValid(block, depth))
                    return false;
            }
        }

        return true;
    }

    private static bool HasSections(BlockInputDto block) => block.Sections is { Count: > 0 };

    private static List<RenderedBlockItemDto>? RenderItems(List<LayoutBlockItem>? items, ILayoutRenderContext ctx) =>
        items?.Select(i => new RenderedBlockItemDto(
            i.Icon, i.Number, i.Title, i.Description,
            i.Description != null ? ctx.Markdown(i.Description) : null,
            ctx.InlineMarkdown(i.Title)
        )).ToList();

    private static void FlattenItems(BlockInputDto block, LayoutMarkdownWriter md)
    {
        if (!string.IsNullOrWhiteSpace(block.Heading))
        {
            md.Separate();
            md.Text.AppendLine($"## {block.Heading}");
        }
        if (!string.IsNullOrWhiteSpace(block.Content))
        {
            md.Separate();
            md.Text.Append(block.Content);
        }
        if (block.Items != null)
        {
            foreach (var item in block.Items)
            {
                md.Separate();
                var prefix = item.Number != null ? $"{item.Number}. " : "- ";
                md.Text.AppendLine($"{prefix}**{item.Title}**: {item.Description}");
            }
        }
    }
}
