namespace SharpQuill.Application.Media.DTOs;

/// <summary>A place where a media item is still referenced; see <see cref="SharpQuill.Core.Media.MediaUsageKind"/> for the kinds.</summary>
public record MediaUsageDto(string Kind, int? ArticleId, string Title, bool IsPage);

public enum MediaDeleteStatus
{
    Deleted,
    NotFound,
    InUse
}

/// <param name="Usages">Set when <see cref="Status"/> is <see cref="MediaDeleteStatus.InUse"/>.</param>
public record MediaDeleteResult(MediaDeleteStatus Status, IReadOnlyList<MediaUsageDto> Usages);
