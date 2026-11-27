using System.Text.RegularExpressions;
using SharpQuill.Application.Articles.Services;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// The backend's block types (LayoutBlockTypes) and the frontend registry in the shared Nuxt layer must describe the
/// same blocks: a block missing on one side is either rejected by the API or skipped by the website. The frontend
/// registry is the folder list in src/frontend/layers/shared/app/blocks and embeds.
/// </summary>
public class LayoutBlockRegistryTests
{
    private static readonly string LayerAppDir = FindLayerAppDir();

    private static string FindLayerAppDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "frontend", "layers", "shared", "app");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException("src/frontend/layers/shared/app not found above " + AppContext.BaseDirectory);
    }

    // A registry entry is a folder with an index.ts (registry.ts and embeds/registry.ts glob for exactly that)
    private static IEnumerable<string> RegistryFolders(string kind) =>
        Directory.GetDirectories(Path.Combine(LayerAppDir, kind))
            .Where(dir => File.Exists(Path.Combine(dir, "index.ts")))
            .Select(Path.GetFileName)
            .Select(name => name!);

    [Fact]
    public void BlockTypes_MatchTheSharedLayerBlockFolders()
    {
        LayoutBlockTypes.Names.Should().BeEquivalentTo(RegistryFolders("blocks"),
            "every block type needs a folder in layers/shared/app/blocks and an entry in LayoutBlockTypes");
    }

    [Fact]
    public void ComponentNames_MatchTheSharedLayerEmbedFolders()
    {
        LayoutBlockTypes.ComponentNames.Should().BeEquivalentTo(RegistryFolders("embeds"),
            "every embed needs a folder in layers/shared/app/embeds and an entry in LayoutBlockTypes.ComponentNames");
    }

    [Fact]
    public void SectionTypes_MatchTheSharedLayoutTypes()
    {
        var layoutTypes = File.ReadAllText(Path.Combine(LayerAppDir, "types", "layout.ts"));
        var declaration = Regex.Match(layoutTypes, @"export type SectionType = ([^\n]+)").Groups[1].Value;
        var frontendTypes = Regex.Matches(declaration, "'([^']+)'").Select(m => m.Groups[1].Value);

        LayoutBlockTypes.SectionTypes.Should().BeEquivalentTo(frontendTypes);
    }

    [Fact]
    public void MaxNestingDepth_MatchesTheEditorLimit()
    {
        var blockTypes = File.ReadAllText(Path.Combine(LayerAppDir, "blocks", "types.ts"));
        var frontendDepth = Regex.Match(blockTypes, @"export const maxNestingDepth = (\d+)").Groups[1].Value;

        frontendDepth.Should().Be(LayoutBlockTypes.MaxNestingDepth.ToString());
    }

    [Theory]
    [InlineData("hologram")]
    [InlineData("Text")]
    [InlineData("")]
    [InlineData(null)]
    public void Find_UnknownType_ReturnsNull(string? name)
    {
        LayoutBlockTypes.Find(name).Should().BeNull();
    }
}
