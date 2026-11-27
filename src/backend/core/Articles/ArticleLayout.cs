using System.Text.Json.Serialization;

namespace SharpQuill.Core.Articles;

public class ArticleLayout
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("sections")]
    public List<LayoutSection> Sections { get; set; } = new();
}

public class LayoutSection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "full";

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("blocks")]
    public List<LayoutBlock> Blocks { get; set; } = new();
}

public class LayoutBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("mediaId")]
    public int? MediaId { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("sections")]
    public List<LayoutSection>? Sections { get; set; }

    // Link block properties
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("siteName")]
    public string? SiteName { get; set; }

    // Tweet block properties
    [JsonPropertyName("tweetDate")]
    public string? TweetDate { get; set; }

    // Code block properties
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    // Cards/steps block properties
    [JsonPropertyName("heading")]
    public string? Heading { get; set; }

    [JsonPropertyName("items")]
    public List<LayoutBlockItem>? Items { get; set; }

    // CTA block properties
    [JsonPropertyName("buttonText")]
    public string? ButtonText { get; set; }

    [JsonPropertyName("buttonUrl")]
    public string? ButtonUrl { get; set; }

    // Component block properties
    [JsonPropertyName("componentName")]
    public string? ComponentName { get; set; }
}

public class LayoutBlockItem
{
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
