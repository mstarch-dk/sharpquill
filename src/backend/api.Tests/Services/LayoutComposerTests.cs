using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Core.Media;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// LayoutComposer and the block types in LayoutBlockTypes without a database: rendering of every block type (media
/// lookups go to a mocked repository), admin parsing and serialising, flattening to Markdown and validation. Stored
/// layouts the current version does not fully understand (a block type removed since, or a hand-edited row) degrade
/// instead of failing.
/// </summary>
public class LayoutComposerTests
{
    private readonly LayoutComposer _composer = new(mediaRepository: null!, uploadsUrlBase: "/uploads");

    [Fact]
    public async Task ComposeAsync_UnknownStoredBlockType_KeepsOnlyTheType()
    {
        const string json = """
            {"version":1,"sections":[{"type":"full","blocks":[
              {"type":"hologram","content":"<script>alert(1)</script>","url":"javascript:alert(1)"},
              {"type":"text","content":"**still here**"}
            ]}]}
            """;

        var layout = await _composer.ComposeAsync(json);

        var blocks = layout!.Sections.Single().Blocks;
        blocks[0].Should().BeEquivalentTo(new RenderedBlockDto("hologram"));
        blocks[1].Html.Should().Contain("<strong>still here</strong>");
    }

    [Fact]
    public async Task ComposeAsync_ContainerWithoutSections_RendersAnEmptyContainer()
    {
        var layout = await _composer.ComposeAsync("""{"sections":[{"type":"full","blocks":[{"type":"container"}]}]}""");

        layout!.Sections.Single().Blocks.Single().Should().BeEquivalentTo(new RenderedBlockDto("container"));
    }

    [Fact]
    public void FlattenToMarkdown_SkipsUnknownAndTextlessBlocks()
    {
        var layout = new LayoutInputDto([
            new SectionInputDto("full", [
                new BlockInputDto("hologram", Content: "invisible"),
                new BlockInputDto("component", ComponentName: "ContactForm"),
                new BlockInputDto("text", Content: "Visible"),
                new BlockInputDto("container", Sections: [
                    new SectionInputDto("full", [new BlockInputDto("cta", Title: "Nested", Description: "Call")])
                ])
            ])
        ]);

        _composer.FlattenToMarkdown(layout).Should().Be("Visible\n## Nested\nCall".Replace("\n", Environment.NewLine));
    }

    [Fact]
    public void IsValidLayout_RejectsAnUnknownBlockType()
    {
        var layout = new LayoutInputDto([new SectionInputDto("full", [new BlockInputDto("hologram", Content: "x")])]);

        LayoutComposer.IsValidLayout(layout).Should().BeFalse();
    }

    private static (LayoutComposer Composer, Mock<IMediaRepository> Media) ComposerWithMedia(params MediaItem[] items)
    {
        var media = new Mock<IMediaRepository>(MockBehavior.Strict);
        media.Setup(m => m.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> ids) => items.Where(i => ids.Contains(i.Id)).ToList());
        return (new LayoutComposer(media.Object, "/uploads"), media);
    }

    private static async Task<RenderedBlockDto> RenderSingle(LayoutComposer composer, string blockJson)
    {
        var layout = await composer.ComposeAsync($$"""{"sections":[{"type":"full","blocks":[{{blockJson}}]}]}""");
        return layout!.Sections.Single().Blocks.Single();
    }

    // --- ComposeAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("""{"version":1}""")]
    [InlineData("""{"sections":[]}""")]
    [InlineData("null")]
    public async Task ComposeAsync_NoSections_ReturnsNull(string? json)
    {
        (await _composer.ComposeAsync(json)).Should().BeNull();
    }

    [Fact]
    public async Task ComposeAsync_KeepsSectionTypeAndStyle()
    {
        var layout = await _composer.ComposeAsync(
            """{"sections":[{"type":"two-column","style":"muted","blocks":[{"type":"text","content":"a"},{"type":"text","content":"b"}]}]}""");

        var section = layout!.Sections.Single();
        section.Type.Should().Be("two-column");
        section.Style.Should().Be("muted");
        section.Blocks.Should().HaveCount(2);
    }

    [Fact]
    public async Task ComposeAsync_ImageBlocks_ResolveMediaInOneBatchIncludingNestedContainers()
    {
        var (composer, media) = ComposerWithMedia(
            new MediaItem { Id = 1, Path = "2026/a.jpg" },
            new MediaItem { Id = 2, Path = "2026/b.png" });

        var layout = await composer.ComposeAsync("""
            {"sections":[{"type":"full","blocks":[
              {"type":"image","mediaId":1,"alt":"A","caption":"*First*"},
              {"type":"image"},
              {"type":"hologram","mediaId":99},
              {"type":"container","sections":[{"type":"full","blocks":[
                {"type":"image","mediaId":2,"alt":"B"},
                {"type":"image","mediaId":3}
              ]}]}
            ]}]}
            """);

        media.Verify(m => m.GetByIdsAsync(It.Is<IEnumerable<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 1, 2, 3 }))),
            Times.Once);
        var blocks = layout!.Sections.Single().Blocks;
        blocks[0].Should().BeEquivalentTo(new RenderedBlockDto("image", ImageUrl: "/uploads/2026/a.jpg", Alt: "A",
            Caption: "*First*", CaptionHtml: "<em>First</em>"));
        blocks[1].ImageUrl.Should().BeNull("an image block without a media id has nothing to show");
        blocks[1].CaptionHtml.Should().BeNull();
        var nested = blocks[3].Sections!.Single().Blocks;
        nested[0].ImageUrl.Should().Be("/uploads/2026/b.png");
        nested[1].ImageUrl.Should().BeNull("media 3 no longer exists");
    }

    [Fact]
    public async Task ComposeAsync_WithoutMedia_DoesNotQueryTheRepository()
    {
        var (composer, media) = ComposerWithMedia();

        await composer.ComposeAsync("""{"sections":[{"type":"full","blocks":[{"type":"text","content":"x"}]}]}""");

        media.Verify(m => m.GetByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [Fact]
    public async Task Text_IsRenderedAndSanitised()
    {
        var block = await RenderSingle(_composer,
            """{"type":"text","content":"# Hi\n\n<script>alert(1)</script>\n\n[x](javascript:alert(1))"}""");

        block.Html.Should().Contain("<h1").And.Contain("Hi").And.NotContain("<script").And.NotContain("javascript:");
    }

    [Theory]
    [InlineData("small", true)]
    [InlineData("huge", false)]
    public async Task Text_KeepsOnlyKnownImageSizes(string size, bool kept)
    {
        var block = await RenderSingle(_composer,
            $$"""{"type":"text","content":"<img src=\"/a.png\" data-size=\"{{size}}\"> <span data-size=\"small\">s</span>"}""");

        block.Html!.Contains($"data-size=\"{size}\"").Should().Be(kept);
        block.Html.Should().NotContain("<span data-size", "data-size is only kept on images");
    }

    [Fact]
    public async Task Text_WithoutContent_RendersEmptyHtml()
    {
        (await RenderSingle(_composer, """{"type":"text"}""")).Html.Should().BeEmpty();
    }

    [Theory]
    [InlineData("csharp", "<pre><code class=\"language-csharp\">a &lt; b</code></pre>")]
    [InlineData(null, "<pre><code>a &lt; b</code></pre>")]
    [InlineData(" ", "<pre><code>a &lt; b</code></pre>")]
    public async Task Code_IsEscapedWithAnOptionalLanguageClass(string? language, string expected)
    {
        var languageJson = language == null ? "" : $",\"language\":\"{language}\"";

        var block = await RenderSingle(_composer, $$"""{"type":"code","content":"a < b"{{languageJson}}}""");

        block.Html.Should().Be(expected);
        block.Language.Should().Be(language);
    }

    [Fact]
    public async Task Code_WithoutContent_RendersAnEmptyBlock()
    {
        (await RenderSingle(_composer, """{"type":"code"}""")).Html.Should().Be("<pre><code></code></pre>");
    }

    [Fact]
    public async Task Link_DropsUnsafeUrls()
    {
        var safe = await RenderSingle(_composer,
            """{"type":"link","url":"https://example.com","title":"T","description":"D","imageUrl":"https://example.com/i.png","siteName":"S"}""");
        var unsafeLink = await RenderSingle(_composer,
            """{"type":"link","url":"javascript:alert(1)","imageUrl":"data:text/html,x"}""");

        safe.Should().BeEquivalentTo(new RenderedBlockDto("link", Url: "https://example.com", Title: "T",
            Description: "D", ImageUrl: "https://example.com/i.png", SiteName: "S"));
        unsafeLink.Url.Should().BeNull();
        unsafeLink.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Tweet_KeepsItsPreviewFields()
    {
        var block = await RenderSingle(_composer,
            """{"type":"tweet","url":"https://x.com/a/status/1","title":"Ann","description":"Hello","siteName":"@ann","imageUrl":"javascript:x","tweetDate":"Nov 1"}""");

        block.Should().BeEquivalentTo(new RenderedBlockDto("tweet", Url: "https://x.com/a/status/1", Title: "Ann",
            Description: "Hello", SiteName: "@ann", ImageUrl: null, TweetDate: "Nov 1"));
    }

    [Fact]
    public async Task Cards_RenderHeadingContentAndItems()
    {
        var block = await RenderSingle(_composer, """
            {"type":"cards","heading":"**Why**","content":"Intro","items":[
              {"icon":"star","title":"Fast","description":"*Very*"},
              {"title":"Plain"}
            ]}
            """);

        block.HeadingHtml.Should().Be("<strong>Why</strong>");
        block.Html.Should().Contain("<p>Intro</p>");
        block.Items.Should().BeEquivalentTo(new[]
        {
            new RenderedBlockItemDto("star", null, "Fast", "*Very*", "<p><em>Very</em></p>\n", "Fast"),
            new RenderedBlockItemDto(null, null, "Plain", null, null, "Plain")
        });
    }

    [Fact]
    public async Task Cards_WithoutContentOrItems_RenderNulls()
    {
        var block = await RenderSingle(_composer, """{"type":"cards"}""");

        block.Html.Should().BeNull();
        block.Items.Should().BeNull();
        block.HeadingHtml.Should().BeNull();
    }

    [Fact]
    public async Task Steps_RenderNumberedItems()
    {
        var block = await RenderSingle(_composer,
            """{"type":"steps","heading":"How","items":[{"number":"1","title":"Plan","description":"Think"}]}""");

        block.Heading.Should().Be("How");
        block.HeadingHtml.Should().Be("How");
        block.Items!.Single().Number.Should().Be("1");
        block.Items!.Single().DescriptionHtml.Should().Contain("Think");
    }

    [Fact]
    public async Task Cta_RendersMarkdownAndDropsAnUnsafeButtonUrl()
    {
        var withDescription = await RenderSingle(_composer,
            """{"type":"cta","title":"Join *now*","description":"It is **free**","buttonText":"Go","buttonUrl":"https://example.com"}""");
        var bare = await RenderSingle(_composer, """{"type":"cta","title":"Hi","buttonUrl":"javascript:alert(1)"}""");

        withDescription.TitleHtml.Should().Be("Join <em>now</em>");
        withDescription.DescriptionHtml.Should().Contain("<strong>free</strong>");
        withDescription.ButtonUrl.Should().Be("https://example.com");
        bare.DescriptionHtml.Should().BeNull();
        bare.ButtonUrl.Should().BeNull();
    }

    [Fact]
    public async Task InlineMarkdown_KeepsParagraphsOfAMultiParagraphText()
    {
        var block = await RenderSingle(_composer, """{"type":"cta","title":"One\n\nTwo"}""");

        block.TitleHtml.Should().Be("<p>One</p>\n<p>Two</p>");
    }

    [Fact]
    public async Task Component_KeepsItsName()
    {
        (await RenderSingle(_composer, """{"type":"component","componentName":"ContactForm"}"""))
            .Should().BeEquivalentTo(new RenderedBlockDto("component", ComponentName: "ContactForm"));
    }

    // --- ParseForAdmin / SerializeLayout ---

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("null")]
    public void ParseForAdmin_NoSections_ReturnsNull(string? json)
    {
        _composer.ParseForAdmin(json).Should().BeNull();
    }

    [Fact]
    public void SerializeLayout_NullOrEmpty_ReturnsNull()
    {
        _composer.SerializeLayout(null).Should().BeNull();
        _composer.SerializeLayout(new LayoutInputDto([])).Should().BeNull();
    }

    [Fact]
    public void SerializeThenParse_RoundTripsEveryField()
    {
        var layout = new LayoutInputDto([
            new SectionInputDto("two-column", [
                new BlockInputDto("image", MediaId: 7, Alt: "alt", Caption: "cap"),
                new BlockInputDto("link", Url: "https://e.com", Title: "t", Description: "d", ImageUrl: "https://e.com/i",
                    SiteName: "s"),
                new BlockInputDto("tweet", Url: "https://x.com/a/status/1", TweetDate: "Nov 1"),
                new BlockInputDto("code", Content: "x", Language: "js"),
                new BlockInputDto("cards", Heading: "h", Items: [new LayoutBlockItemDto("i", "1", "t", "d")]),
                new BlockInputDto("cta", Title: "c", ButtonText: "b", ButtonUrl: "https://e.com"),
                new BlockInputDto("component", ComponentName: "ContactForm"),
                new BlockInputDto("container", Sections: [
                    new SectionInputDto("full", [new BlockInputDto("text", Content: "nested")], "inner")
                ])
            ], "outer")
        ]);

        var json = _composer.SerializeLayout(layout);
        var parsed = _composer.ParseForAdmin(json);

        parsed.Should().BeEquivalentTo(layout);
    }

    [Fact]
    public void ParseForAdmin_ContainerWithoutSections_IsParsedAsAPlainBlock()
    {
        var parsed = _composer.ParseForAdmin("""{"sections":[{"type":"full","blocks":[{"type":"container"}]}]}""");

        parsed!.Sections.Single().Blocks.Single().Sections.Should().BeNull();
    }

    // --- FlattenToMarkdown ---

    [Fact]
    public void FlattenToMarkdown_NullOrEmpty_ReturnsNull()
    {
        _composer.FlattenToMarkdown(null).Should().BeNull();
        _composer.FlattenToMarkdown(new LayoutInputDto([])).Should().BeNull();
        _composer.FlattenToMarkdown(new LayoutInputDto([
            new SectionInputDto("full", [new BlockInputDto("image", MediaId: 1), new BlockInputDto("text", Content: " ")])
        ])).Should().BeNull("blocks without text produce no Markdown");
    }

    [Fact]
    public void FlattenToMarkdown_WritesEveryTextBearingBlockType()
    {
        var layout = new LayoutInputDto([
            new SectionInputDto("full", [
                new BlockInputDto("code", Content: "x = 1", Language: "py"),
                new BlockInputDto("code", Content: "plain"),
                new BlockInputDto("code"),
                new BlockInputDto("link", Url: "https://e.com", Title: "E"),
                new BlockInputDto("link", Url: "https://f.com"),
                new BlockInputDto("link"),
                new BlockInputDto("tweet", Url: "https://x.com/a/status/1"),
                new BlockInputDto("tweet"),
                new BlockInputDto("cards", Heading: "Cards", Content: "Intro", Items: [
                    new LayoutBlockItemDto(null, null, "A", "a"),
                    new LayoutBlockItemDto(null, "2", "B", "b")
                ]),
                new BlockInputDto("steps"),
                new BlockInputDto("cta", Description: "Only a description"),
                new BlockInputDto("container")
            ])
        ]);

        var expected = string.Join(Environment.NewLine,
            "```py", "x = 1", "```",
            "```", "plain", "```",
            "[E](https://e.com)",
            "[https://f.com](https://f.com)",
            "[Tweet](https://x.com/a/status/1)",
            "## Cards", "",
            "Intro",
            "- **A**: a", "",
            "2. **B**: b", "Only a description");
        _composer.FlattenToMarkdown(layout).Should().Be(expected);
    }

    // --- IsValidLayout ---

    [Fact]
    public void IsValidLayout_NullIsValidAndEmptyIsNot()
    {
        LayoutComposer.IsValidLayout(null).Should().BeTrue("no layout means a plain Markdown article");
        LayoutComposer.IsValidLayout(new LayoutInputDto([])).Should().BeFalse();
    }

    public static TheoryData<BlockInputDto, bool> BlockValidity()
    {
        var nested = new List<SectionInputDto> { new("full", [new BlockInputDto("text", Content: "x")]) };
        var items = new List<LayoutBlockItemDto> { new(null, null, "t", null) };
        return new TheoryData<BlockInputDto, bool>
        {
            { new BlockInputDto("text", Content: "x"), true },
            { new BlockInputDto("text", Content: " "), false },
            { new BlockInputDto("text", Content: "x", Sections: nested), false },
            { new BlockInputDto("image", MediaId: 1), true },
            { new BlockInputDto("image"), false },
            { new BlockInputDto("image", MediaId: 1, Sections: nested), false },
            { new BlockInputDto("code", Content: "x"), true },
            { new BlockInputDto("code"), false },
            { new BlockInputDto("code", Content: "x", MediaId: 1), false },
            { new BlockInputDto("code", Content: "x", Sections: nested), false },
            { new BlockInputDto("link", Url: "https://e.com"), true },
            { new BlockInputDto("link"), false },
            { new BlockInputDto("link", Url: "https://e.com", MediaId: 1), false },
            { new BlockInputDto("link", Url: "https://e.com", Sections: nested), false },
            { new BlockInputDto("link", Url: "javascript:alert(1)"), false },
            { new BlockInputDto("link", Url: "https://e.com", ImageUrl: "data:text/html,x"), false },
            { new BlockInputDto("cta", Title: "t", ButtonUrl: "vbscript:x"), false },
            { new BlockInputDto("tweet", Url: "https://twitter.com/a/status/1"), true },
            { new BlockInputDto("tweet", Url: "https://x.com/a/status/1"), true },
            { new BlockInputDto("tweet", Url: "https://example.com/a"), false },
            { new BlockInputDto("tweet"), false },
            { new BlockInputDto("tweet", Url: "https://x.com/a", MediaId: 1), false },
            { new BlockInputDto("tweet", Url: "https://x.com/a", Sections: nested), false },
            { new BlockInputDto("container", Sections: nested), true },
            { new BlockInputDto("container"), false },
            { new BlockInputDto("container", Sections: []), false },
            { new BlockInputDto("container", Content: "x", Sections: nested), false },
            { new BlockInputDto("container", MediaId: 1, Sections: nested), false },
            { new BlockInputDto("container", Sections: [new SectionInputDto("full", [new BlockInputDto("text")])]), false },
            { new BlockInputDto("cards", Items: items), true },
            { new BlockInputDto("cards"), false },
            { new BlockInputDto("cards", Items: []), false },
            { new BlockInputDto("cards", Items: items, Sections: nested), false },
            { new BlockInputDto("steps", Items: items), true },
            { new BlockInputDto("steps", Items: []), false },
            { new BlockInputDto("steps", Items: items, Sections: nested), false },
            { new BlockInputDto("cta", Title: "t"), true },
            { new BlockInputDto("cta"), false },
            { new BlockInputDto("cta", Title: "t", Sections: nested), false },
            { new BlockInputDto("component", ComponentName: "ContactForm"), true },
            { new BlockInputDto("component", ComponentName: "Unknown"), false },
            { new BlockInputDto("component"), false },
            { new BlockInputDto("component", ComponentName: "ContactForm", Sections: nested), false },
        };
    }

    [Theory]
    [MemberData(nameof(BlockValidity))]
    public void IsValidLayout_AppliesEachBlockTypesRules(BlockInputDto block, bool valid)
    {
        LayoutComposer.IsValidLayout(new LayoutInputDto([new SectionInputDto("full", [block])])).Should().Be(valid);
    }

    [Fact]
    public void IsValidLayout_RejectsUnknownSectionTypesAndEmptySections()
    {
        LayoutComposer.IsValidLayout(new LayoutInputDto([new SectionInputDto("four-column", [new BlockInputDto("text", Content: "x")])]))
            .Should().BeFalse();
        LayoutComposer.IsValidLayout(new LayoutInputDto([new SectionInputDto("full", [])])).Should().BeFalse();
    }

    [Fact]
    public void IsValidLayout_LimitsNestingDepth()
    {
        static IReadOnlyList<SectionInputDto> Nest(int containers)
        {
            IReadOnlyList<SectionInputDto> sections = [new SectionInputDto("full", [new BlockInputDto("text", Content: "x")])];
            for (var i = 0; i < containers; i++)
                sections = [new SectionInputDto("full", [new BlockInputDto("container", Sections: sections)])];
            return sections;
        }

        LayoutComposer.IsValidLayout(new LayoutInputDto(Nest(LayoutBlockTypes.MaxNestingDepth - 1))).Should().BeTrue();
        LayoutComposer.IsValidLayout(new LayoutInputDto(Nest(LayoutBlockTypes.MaxNestingDepth))).Should().BeFalse();
    }
}
