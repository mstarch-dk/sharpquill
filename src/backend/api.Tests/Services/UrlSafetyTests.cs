using SharpQuill.Application.Common;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

public class UrlSafetyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/a?b=c:d")]
    [InlineData("HTTP://example.com")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("tel:+4512345678")]
    [InlineData("/blog/post")]
    [InlineData("blog/post")]
    [InlineData("//cdn.example.com/x.png")]
    [InlineData("#section")]
    [InlineData("#a:b")]
    [InlineData("/path:with-colon")]
    [InlineData("?q=a:b")]
    [InlineData("https://example.com/?x=1&amp;y=2")]
    [InlineData("/p?x=1&y=2")]
    [InlineData("/a#b&c")]
    public void IsSafe_AcceptsNormalUrls(string? url)
    {
        UrlSafety.IsSafe(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("  javascript:alert(1)")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("java\nscript:alert(1)")]
    [InlineData("\u0001javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    [InlineData("&#106;avascript:alert(1)")]
    [InlineData("&#x6A;avascript:alert(1)")]
    [InlineData("&#0000106avascript:alert(1)")]
    [InlineData("javascript&colon;alert(1)")]
    [InlineData("java&Tab;script:alert(1)")]
    [InlineData("java&NewLine;script:alert(1)")]
    [InlineData("javascript&unknown;:alert(1)")]
    public void IsSafe_RejectsScriptCapableAndUnknownSchemes(string url)
    {
        UrlSafety.IsSafe(url).Should().BeFalse();
        UrlSafety.OrNull(url).Should().BeNull();
    }
}
