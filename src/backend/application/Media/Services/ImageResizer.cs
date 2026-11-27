using SkiaSharp;

namespace SharpQuill.Application.Media.Services;

public interface IImageResizer
{
    bool IsRasterImage(string mimeType);
    (int Width, int Height)? GetDimensions(string filePath);
    bool TryCreateVariant(string sourceFilePath, string outputFilePath, int maxWidth, string mimeType);
}

public class ImageResizer : IImageResizer
{
    /// <summary>Images with more pixels than this are never decoded (decompression-bomb guard, ~40 megapixels).</summary>
    public const long MaxPixels = 40_000_000;

    public static bool ExceedsPixelLimit(int width, int height) => (long)width * height > MaxPixels;

    private static readonly HashSet<string> RasterMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public bool IsRasterImage(string mimeType) => RasterMimeTypes.Contains(mimeType);

    public (int Width, int Height)? GetDimensions(string filePath)
    {
        try
        {
            using var codec = SKCodec.Create(filePath);
            if (codec == null) return null;
            var info = codec.Info;
            return (info.Width, info.Height);
        }
        catch
        {
            return null;
        }
    }

    public bool TryCreateVariant(string sourceFilePath, string outputFilePath, int maxWidth, string mimeType)
    {
        try
        {
            // Read the declared size from the header before allocating a bitmap for it
            var dims = GetDimensions(sourceFilePath);
            if (dims == null || ExceedsPixelLimit(dims.Value.Width, dims.Value.Height)) return false;

            using var original = SKBitmap.Decode(sourceFilePath);
            if (original == null) return false;

            // Don't upscale
            if (original.Width <= maxWidth) return false;

            var scale = (float)maxWidth / original.Width;
            var newHeight = (int)Math.Round(original.Height * scale);

            using var scaled = original.Resize(new SKImageInfo(maxWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            if (scaled == null) return false;

            var format = GetFormat(mimeType);
            var quality = format == SKEncodedImageFormat.Png ? 100 : 85;

            using var image = SKImage.FromBitmap(scaled);
            using var data = image.Encode(format, quality);
            using var output = File.Create(outputFilePath);
            data.SaveTo(output);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static SKEncodedImageFormat GetFormat(string mimeType) =>
        mimeType.ToLowerInvariant() switch
        {
            "image/png" => SKEncodedImageFormat.Png,
            "image/gif" => SKEncodedImageFormat.Gif,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };
}
