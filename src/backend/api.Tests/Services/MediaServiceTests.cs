using SharpQuill.Application.Media.DTOs;
using SharpQuill.Application.Media.Services;
using SharpQuill.Core.Media;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// MediaService branches the upload and delete round trips in AdminMediaControllerTests do not reach: oversized files,
/// images without dimensions or variants, concurrent deletes, stored paths outside the uploads folder and the
/// variant backfill.
/// </summary>
public sealed class MediaServiceTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

    private readonly string _uploads = Directory.CreateTempSubdirectory("cms-media-").FullName;
    private readonly Mock<IMediaRepository> _repository = new();
    private readonly Mock<IImageResizer> _resizer = new();
    private readonly MediaService _service;

    public MediaServiceTests()
    {
        _repository.Setup(r => r.CreateAsync(It.IsAny<MediaItem>())).ReturnsAsync((MediaItem m) => { m.Id = 1; return m; });
        _resizer.Setup(r => r.IsRasterImage("image/png")).Returns(true);
        _service = new MediaService(_repository.Object, _resizer.Object, _uploads, "/uploads");
    }

    public void Dispose() => Directory.Delete(_uploads, recursive: true);

    [Fact]
    public async Task Upload_LargerThanTheLimit_IsRefusedBeforeReading()
    {
        var (result, error) = await _service.UploadAsync(Stream.Null, "big.png", 10 * 1024 * 1024 + 1, 1);

        result.Should().BeNull();
        error.Should().Be("File size exceeds the maximum allowed size of 10 MB");
    }

    [Fact]
    public async Task Upload_ImageWithoutReadableDimensionsOrVariants_IsStoredWithoutThem()
    {
        _resizer.Setup(r => r.GetDimensions(It.IsAny<string>())).Returns(((int, int)?)null);
        _resizer.Setup(r => r.TryCreateVariant(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(false);

        var (result, error) = await _service.UploadAsync(new MemoryStream(Png), "a.png", Png.Length, 1);

        error.Should().BeNull();
        result!.ThumbnailUrl.Should().BeNull();
        result.MediumUrl.Should().BeNull();
        _repository.Verify(r => r.CreateAsync(It.Is<MediaItem>(m => m.Width == null && m.Height == null)));
    }

    [Fact]
    public async Task Delete_RemovedConcurrently_ReportsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new MediaItem { Id = 5, Path = "a.png" });
        _repository.Setup(r => r.DeleteAsync(5)).ReturnsAsync(false);

        (await _service.DeleteAsync(5, force: true)).Status.Should().Be(MediaDeleteStatus.NotFound);
    }

    [Fact]
    public async Task Delete_NeverFollowsAStoredPathOutsideTheUploadsFolder()
    {
        var outside = Path.Combine(Path.GetDirectoryName(_uploads)!, $"outside-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(outside, Png);
        try
        {
            _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new MediaItem
            {
                Id = 5, Path = "../" + Path.GetFileName(outside), MediumPath = null, ThumbnailPath = "missing_thumb.png"
            });
            _repository.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

            (await _service.DeleteAsync(5, force: true)).Status.Should().Be(MediaDeleteStatus.Deleted);

            File.Exists(outside).Should().BeTrue();
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task RegenerateVariants_UpdatesExistingFilesAndSkipsMissingOnes()
    {
        Directory.CreateDirectory(Path.Combine(_uploads, "2026"));
        await File.WriteAllBytesAsync(Path.Combine(_uploads, "2026", "a.png"), Png);
        await File.WriteAllBytesAsync(Path.Combine(_uploads, "b.png"), Png);
        _repository.Setup(r => r.GetRasterMediaWithoutVariantsAsync()).ReturnsAsync([
            new MediaItem { Id = 1, Filename = "a.png", Path = "2026/a.png", MimeType = "image/png" },
            new MediaItem { Id = 2, Filename = "b.png", Path = "b.png", MimeType = "image/png" },
            new MediaItem { Id = 3, Filename = "gone.png", Path = "2026/gone.png", MimeType = "image/png" }
        ]);
        _resizer.Setup(r => r.GetDimensions(It.Is<string>(p => p.EndsWith("a.png")))).Returns((1200, 600));
        _resizer.Setup(r => r.TryCreateVariant(It.Is<string>(p => p.EndsWith("a.png")), It.IsAny<string>(), It.IsAny<int>(), "image/png"))
            .Returns(true);

        var count = await _service.RegenerateVariantsAsync();

        count.Should().Be(2);
        _repository.Verify(r => r.UpdateVariantPathsAsync(1, "2026/a_thumb.png", "2026/a_medium.png", 1200, 600));
        _repository.Verify(r => r.UpdateVariantPathsAsync(2, null, null, null, null));
        _repository.Verify(r => r.UpdateVariantPathsAsync(3, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>()),
            Times.Never);
    }
}
