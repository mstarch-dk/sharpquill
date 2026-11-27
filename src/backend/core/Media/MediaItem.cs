using SharpQuill.Core.Authors;

namespace SharpQuill.Core.Media;

public class MediaItem
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? MediumPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
