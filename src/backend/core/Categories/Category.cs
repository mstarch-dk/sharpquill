using SharpQuill.Core.Articles;

namespace SharpQuill.Core.Categories;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
