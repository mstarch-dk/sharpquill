namespace SharpQuill.Application.Articles.Services;

public static class FeaturedImageVariantResolver
{
    public static string NormalizeFeaturedImageSize(string? size)
    {
        return size?.Trim().ToLowerInvariant() switch
        {
            "extra-small" => "extra-small",
            "small" => "small",
            "original" => "original",
            _ => "medium"
        };
    }

    public static string ResolveDisplayUrl(
        string requestedSize,
        string originalUrl,
        string? thumbnailUrl,
        string? mediumUrl)
    {
        return NormalizeFeaturedImageSize(requestedSize) switch
        {
            "extra-small" or "small" => thumbnailUrl ?? mediumUrl ?? originalUrl,
            "original" => originalUrl,
            _ => mediumUrl ?? originalUrl
        };
    }
}
