namespace SharpQuill.Application.Media.Services;

public record DetectedMediaType(string MimeType, string Extension, string[] AcceptedExtensions);

/// <summary>
/// Identifies allowed media types from their file signature (magic bytes), so stored files never
/// depend on a client-supplied Content-Type or extension. Script-capable formats such as SVG and
/// HTML are deliberately not recognised.
/// </summary>
public static class MediaTypeDetector
{
    public const int HeaderLength = 12;

    public const string AllowedTypesDescription = "JPEG, PNG, GIF, WebP, PDF";

    private static readonly DetectedMediaType Jpeg = new("image/jpeg", ".jpg", [".jpg", ".jpeg"]);
    private static readonly DetectedMediaType Png = new("image/png", ".png", [".png"]);
    private static readonly DetectedMediaType Gif = new("image/gif", ".gif", [".gif"]);
    private static readonly DetectedMediaType Webp = new("image/webp", ".webp", [".webp"]);
    private static readonly DetectedMediaType Pdf = new("application/pdf", ".pdf", [".pdf"]);

    public static DetectedMediaType? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return Jpeg;
        if (header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return Png;
        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
            return Gif;
        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
            return Webp;
        if (header.StartsWith("%PDF-"u8))
            return Pdf;
        return null;
    }

    /// <summary>
    /// Reads up to <see cref="HeaderLength"/> bytes from the stream for detection. The returned
    /// buffer holds the bytes consumed and must be written before the rest of the stream.
    /// </summary>
    public static async Task<byte[]> ReadHeaderAsync(Stream stream)
    {
        var buffer = new byte[HeaderLength];
        var total = 0;
        int read;
        while (total < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(total))) > 0)
        {
            total += read;
        }
        return buffer[..total];
    }
}
