using SharpQuill.Application.Common.DTOs;
using SharpQuill.Application.Media.DTOs;
using SharpQuill.Core.Common;
using SharpQuill.Core.Media;

namespace SharpQuill.Application.Media.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IImageResizer _imageResizer;
    private readonly string _uploadsPath;
    private readonly string _uploadsUrlBase;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public MediaService(IMediaRepository mediaRepository, IImageResizer imageResizer, string uploadsPath, string uploadsUrlBase)
    {
        _mediaRepository = mediaRepository;
        _imageResizer = imageResizer;
        _uploadsPath = uploadsPath;
        _uploadsUrlBase = uploadsUrlBase;
    }

    public async Task<MediaDto?> GetByIdAsync(int id)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        return media == null ? null : MapToMediaDto(media);
    }

    public async Task<PagedResultDto<MediaDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, totalCount) = await _mediaRepository.GetAllAsync(pagination);

        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = items.Select(MapToMediaDto).ToList();
        var meta = new PaginationMeta(page, pageSize, totalCount, totalPages);

        return new PagedResultDto<MediaDto>(data, meta);
    }

    public async Task<(MediaUploadResultDto? Result, string? Error)> UploadAsync(
        Stream fileStream, string originalFilename, long size, int authorId)
    {
        if (size > MaxFileSize)
        {
            return (null, $"File size exceeds the maximum allowed size of {MaxFileSize / 1024 / 1024} MB");
        }

        // The stored type and extension come from the file content, never from the client
        var header = await MediaTypeDetector.ReadHeaderAsync(fileStream);
        var detected = MediaTypeDetector.Detect(header);
        if (detected == null)
        {
            return (null, $"File type is not allowed. Allowed types: {MediaTypeDetector.AllowedTypesDescription}");
        }
        var mimeType = detected.MimeType;

        // Generate unique filename
        var extension = detected.Extension;
        var filenameGuid = Guid.NewGuid().ToString("N");
        var filename = $"{filenameGuid}{extension}";

        // Create year/month subdirectory
        var now = DateTime.UtcNow;
        var subDirectory = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"));
        var fullDirectory = Path.Combine(_uploadsPath, subDirectory);

        // Ensure directory exists
        Directory.CreateDirectory(fullDirectory);

        var filePath = Path.Combine(fullDirectory, filename);
        var relativePath = Path.Combine(subDirectory, filename).Replace("\\", "/");

        // Save file
        await using (var fileStreamOut = new FileStream(filePath, FileMode.Create))
        {
            await fileStreamOut.WriteAsync(header);
            await fileStream.CopyToAsync(fileStreamOut);
        }

        // Generate image variants for raster images
        string? thumbnailPath = null, mediumPath = null;
        int? width = null, height = null;

        if (_imageResizer.IsRasterImage(mimeType))
        {
            var dims = _imageResizer.GetDimensions(filePath);
            if (dims.HasValue && ImageResizer.ExceedsPixelLimit(dims.Value.Width, dims.Value.Height))
            {
                File.Delete(filePath);
                return (null, $"Image is too large (more than {ImageResizer.MaxPixels / 1_000_000} megapixels)");
            }
            if (dims.HasValue)
            {
                width = dims.Value.Width;
                height = dims.Value.Height;
            }

            var thumbFilename = $"{filenameGuid}_thumb{extension}";
            if (_imageResizer.TryCreateVariant(filePath, Path.Combine(fullDirectory, thumbFilename), 400, mimeType))
                thumbnailPath = Path.Combine(subDirectory, thumbFilename).Replace("\\", "/");

            var mediumFilename = $"{filenameGuid}_medium{extension}";
            if (_imageResizer.TryCreateVariant(filePath, Path.Combine(fullDirectory, mediumFilename), 800, mimeType))
                mediumPath = Path.Combine(subDirectory, mediumFilename).Replace("\\", "/");
        }

        // Create database record
        var media = new MediaItem
        {
            Filename = filename,
            OriginalFilename = originalFilename,
            MimeType = mimeType,
            Size = size,
            Path = relativePath,
            ThumbnailPath = thumbnailPath,
            MediumPath = mediumPath,
            Width = width,
            Height = height,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _mediaRepository.CreateAsync(media);

        var result = new MediaUploadResultDto(
            created.Id,
            created.Filename,
            created.OriginalFilename,
            created.MimeType,
            created.Size,
            GetFileUrl(created.Path),
            created.ThumbnailPath != null ? GetFileUrl(created.ThumbnailPath) : null,
            created.MediumPath != null ? GetFileUrl(created.MediumPath) : null
        );

        return (result, null);
    }

    public async Task<IReadOnlyList<MediaUsageDto>?> GetUsagesAsync(int id)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        return media == null ? null : await GetUsagesAsync(media);
    }

    public async Task<MediaDeleteResult> DeleteAsync(int id, bool force = false)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        if (media == null)
        {
            return new MediaDeleteResult(MediaDeleteStatus.NotFound, []);
        }

        if (!force)
        {
            var usages = await GetUsagesAsync(media);
            if (usages.Count > 0)
            {
                return new MediaDeleteResult(MediaDeleteStatus.InUse, usages);
            }
        }

        if (!await _mediaRepository.DeleteAsync(id))
        {
            // Deleted concurrently
            return new MediaDeleteResult(MediaDeleteStatus.NotFound, []);
        }

        foreach (var path in new[] { media.Path, media.MediumPath, media.ThumbnailPath })
        {
            DeleteUploadedFile(path);
        }

        return new MediaDeleteResult(MediaDeleteStatus.Deleted, []);
    }

    private async Task<IReadOnlyList<MediaUsageDto>> GetUsagesAsync(MediaItem media)
    {
        var usages = await _mediaRepository.GetUsagesAsync(media);
        return usages.Select(u => new MediaUsageDto(u.Kind, u.ArticleId, u.Title, u.IsPage)).ToList();
    }

    private void DeleteUploadedFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        // Stored paths are generated by the API, but never follow one outside the uploads folder
        var root = Path.GetFullPath(_uploadsPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return;

        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public async Task<int> RegenerateVariantsAsync()
    {
        var items = await _mediaRepository.GetRasterMediaWithoutVariantsAsync();
        var count = 0;

        foreach (var media in items)
        {
            var filePath = Path.Combine(_uploadsPath, media.Path);
            if (!File.Exists(filePath)) continue;

            var dims = _imageResizer.GetDimensions(filePath);
            var ext = Path.GetExtension(media.Filename);
            var filenameBase = Path.GetFileNameWithoutExtension(media.Filename);
            var subDirectory = Path.GetDirectoryName(media.Path)?.Replace("\\", "/") ?? "";
            var fullDirectory = Path.Combine(_uploadsPath, subDirectory);

            string? thumbnailPath = null, mediumPath = null;

            var thumbFilename = $"{filenameBase}_thumb{ext}";
            if (_imageResizer.TryCreateVariant(filePath, Path.Combine(fullDirectory, thumbFilename), 400, media.MimeType))
                thumbnailPath = $"{subDirectory}/{thumbFilename}";

            var mediumFilename = $"{filenameBase}_medium{ext}";
            if (_imageResizer.TryCreateVariant(filePath, Path.Combine(fullDirectory, mediumFilename), 800, media.MimeType))
                mediumPath = $"{subDirectory}/{mediumFilename}";

            await _mediaRepository.UpdateVariantPathsAsync(media.Id, thumbnailPath, mediumPath, dims?.Width, dims?.Height);
            count++;
        }

        return count;
    }

    private MediaDto MapToMediaDto(MediaItem media)
    {
        return new MediaDto(
            media.Id,
            media.Filename,
            media.OriginalFilename,
            media.MimeType,
            media.Size,
            GetFileUrl(media.Path),
            media.ThumbnailPath != null ? GetFileUrl(media.ThumbnailPath) : null,
            media.MediumPath != null ? GetFileUrl(media.MediumPath) : null,
            media.Width,
            media.Height,
            media.AltText,
            media.CreatedAt,
            new MediaAuthorDto(media.Author.Id, media.Author.Name)
        );
    }

    private string GetFileUrl(string path)
    {
        return $"{_uploadsUrlBase}/{path}";
    }
}
