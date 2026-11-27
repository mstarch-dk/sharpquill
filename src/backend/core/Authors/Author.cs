using SharpQuill.Core.Articles;
using SharpQuill.Core.Media;

namespace SharpQuill.Core.Authors;

public class Author
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
    public ICollection<MediaItem> Media { get; set; } = new List<MediaItem>();
}
