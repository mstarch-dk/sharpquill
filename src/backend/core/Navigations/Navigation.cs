namespace SharpQuill.Core.Navigations;

public class Navigation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<NavigationItem> Items { get; set; } = new List<NavigationItem>();
}
