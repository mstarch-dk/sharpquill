using SharpQuill.Core.Articles;

namespace SharpQuill.Core.Navigations;

public class NavigationItem
{
    public int Id { get; set; }
    public int NavigationId { get; set; }
    public int? ParentId { get; set; }
    public string Label { get; set; } = string.Empty;
    public NavigationItemType ItemType { get; set; }
    public int? ArticleId { get; set; }
    public string? SystemRoute { get; set; }
    public string? Url { get; set; }
    public int Position { get; set; }
    public bool OpenInNewTab { get; set; }

    public Navigation Navigation { get; set; } = null!;
    public NavigationItem? Parent { get; set; }
    public Article? Article { get; set; }
    public ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();
}
