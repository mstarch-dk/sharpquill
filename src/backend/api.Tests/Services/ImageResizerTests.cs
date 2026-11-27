using SharpQuill.Application.Media.Services;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// ImageResizer on real image files: dimensions, downscaled variants per format, and the cases that create no variant.
/// </summary>
public sealed class ImageResizerTests : IDisposable
{
    private readonly ImageResizer _resizer = new();
    private readonly string _dir = Directory.CreateTempSubdirectory("cms-resizer-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteImage(string name, int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private string WriteText(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("IMAGE/PNG", true)]
    [InlineData("image/gif", true)]
    [InlineData("image/webp", true)]
    [InlineData("image/svg+xml", false)]
    [InlineData("application/pdf", false)]
    public void IsRasterImage_KnowsTheResizableTypes(string mimeType, bool expected)
    {
        _resizer.IsRasterImage(mimeType).Should().Be(expected);
    }

    [Fact]
    public void GetDimensions_ReadsTheImageHeader()
    {
        _resizer.GetDimensions(WriteImage("a.png", 30, 20)).Should().Be((30, 20));
    }

    [Fact]
    public void GetDimensions_NotAnImageOrMissing_ReturnsNull()
    {
        _resizer.GetDimensions(WriteText("a.txt", "not an image")).Should().BeNull();
        _resizer.GetDimensions(Path.Combine(_dir, "missing.png")).Should().BeNull();
    }

    [Theory]
    [InlineData("image/png", SKEncodedImageFormat.Png)]
    [InlineData("image/jpeg", SKEncodedImageFormat.Jpeg)]
    [InlineData("image/webp", SKEncodedImageFormat.Webp)]
    [InlineData("image/gif", SKEncodedImageFormat.Png)] // Skia has no GIF encoder; the variant is not written
    public void TryCreateVariant_DownscalesKeepingTheAspectRatio(string mimeType, SKEncodedImageFormat expectedFormat)
    {
        var source = WriteImage("big.png", 200, 100);
        var output = Path.Combine(_dir, "variant");

        var created = _resizer.TryCreateVariant(source, output, 50, mimeType);

        if (mimeType == "image/gif")
        {
            created.Should().BeFalse("encoding to GIF fails, which the resizer reports as no variant");
            return;
        }
        created.Should().BeTrue();
        using var codec = SKCodec.Create(output);
        codec.EncodedFormat.Should().Be(expectedFormat);
        (codec.Info.Width, codec.Info.Height).Should().Be((50, 25));
    }

    [Fact]
    public void TryCreateVariant_NeverUpscales()
    {
        var source = WriteImage("small.png", 40, 40);
        var output = Path.Combine(_dir, "variant.png");

        _resizer.TryCreateVariant(source, output, 40, "image/png").Should().BeFalse();
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void TryCreateVariant_UnreadableSource_ReturnsFalse()
    {
        _resizer.TryCreateVariant(WriteText("x.png", "junk"), Path.Combine(_dir, "v.png"), 10, "image/png")
            .Should().BeFalse();
    }

    [Fact]
    public void TryCreateVariant_UnwritableOutput_ReturnsFalse()
    {
        var source = WriteImage("big.png", 200, 100);

        _resizer.TryCreateVariant(source, Path.Combine(_dir, "no-such-dir", "v.png"), 50, "image/png")
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(8000, 5000, false)]
    [InlineData(8000, 5001, true)]
    [InlineData(int.MaxValue, int.MaxValue, true)]
    public void ExceedsPixelLimit_GuardsAgainstDecompressionBombs(int width, int height, bool expected)
    {
        ImageResizer.ExceedsPixelLimit(width, height).Should().Be(expected);
    }
}
