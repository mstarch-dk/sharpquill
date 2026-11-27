using SharpQuill.Application.Articles.Services;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

public class FeaturedImageVariantResolverTests
{
    [Theory]
    [InlineData(null, "medium")]
    [InlineData("", "medium")]
    [InlineData("medium", "medium")]
    [InlineData("MeDium", "medium")]
    [InlineData("extra-small", "extra-small")]
    [InlineData(" Extra-Small ", "extra-small")]
    [InlineData("small", "small")]
    [InlineData(" SMALL ", "small")]
    [InlineData("original", "original")]
    [InlineData("ORIGINAL", "original")]
    [InlineData("invalid", "medium")]
    public void NormalizeFeaturedImageSize_ShouldNormalize(string? input, string expected)
    {
        var result = FeaturedImageVariantResolver.NormalizeFeaturedImageSize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void ResolveDisplayUrl_ShouldUseThumbnailForSmall_WhenAvailable()
    {
        var result = FeaturedImageVariantResolver.ResolveDisplayUrl(
            requestedSize: "small",
            originalUrl: "/o.jpg",
            thumbnailUrl: "/t.jpg",
            mediumUrl: "/m.jpg"
        );

        result.Should().Be("/t.jpg");
    }

    [Fact]
    public void ResolveDisplayUrl_ShouldFallbackToMediumForSmall_WhenThumbnailMissing()
    {
        var result = FeaturedImageVariantResolver.ResolveDisplayUrl(
            requestedSize: "small",
            originalUrl: "/o.jpg",
            thumbnailUrl: null,
            mediumUrl: "/m.jpg"
        );

        result.Should().Be("/m.jpg");
    }

    [Fact]
    public void ResolveDisplayUrl_ShouldFallbackToOriginalForMedium_WhenMediumMissing()
    {
        var result = FeaturedImageVariantResolver.ResolveDisplayUrl(
            requestedSize: "medium",
            originalUrl: "/o.jpg",
            thumbnailUrl: "/t.jpg",
            mediumUrl: null
        );

        result.Should().Be("/o.jpg");
    }

    [Fact]
    public void ResolveDisplayUrl_ShouldAlwaysUseOriginalForOriginal()
    {
        var result = FeaturedImageVariantResolver.ResolveDisplayUrl(
            requestedSize: "original",
            originalUrl: "/o.jpg",
            thumbnailUrl: "/t.jpg",
            mediumUrl: "/m.jpg"
        );

        result.Should().Be("/o.jpg");
    }
}
