using System.Text.RegularExpressions;

namespace SharpQuill.Application.Common;

/// <summary>
/// Decides whether a user-supplied URL may be rendered as a link or image source.
/// Only http, https, mailto and tel URLs and relative URLs are accepted; script-capable schemes
/// such as javascript:, data: and vbscript: are not.
/// </summary>
public static class UrlSafety
{
    private static readonly string[] AllowedSchemes = ["http", "https", "mailto", "tel"];

    // Browsers decode HTML entities in attribute values before parsing the URL, so "&#106;avascript:" is "javascript:"
    private static readonly Regex Entity = new(@"&(?:#(\d+)|#x([0-9a-f]+)|([a-z][a-z0-9]*));?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UndecodedEntityBeforeDelimiter = new(@"^[^/?#]*&[a-z][a-z0-9]*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> NamedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'", ["colon"] = ":", ["tab"] = "\t",
        ["newline"] = "\n", ["nbsp"] = "\u00a0", ["lpar"] = "(", ["rpar"] = ")", ["sol"] = "/", ["num"] = "#",
        ["quest"] = "?", ["period"] = "."
    };

    private static string DecodeEntities(string value) => Entity.Replace(value, m =>
    {
        if (m.Groups[1].Success || m.Groups[2].Success)
        {
            var parsed = m.Groups[1].Success
                ? long.TryParse(m.Groups[1].Value, out var d) ? d : -1
                : long.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, null, out var h) ? h : -1;
            return parsed is > 0 and <= 0x10FFFF && !(parsed is >= 0xD800 and <= 0xDFFF)
                ? char.ConvertFromUtf32((int)parsed)
                : "";
        }

        return NamedEntities.TryGetValue(m.Groups[3].Value, out var named) ? named : m.Value;
    });

    /// <summary>True for null/empty (nothing to render), relative URLs and URLs with an allowed scheme.</summary>
    public static bool IsSafe(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        // Browsers ignore whitespace and control characters inside the scheme ("java\tscript:"), so do the same
        var compact = new string(DecodeEntities(url).Where(c => c > ' ' && c != '\u007f').ToArray());

        var schemeEnd = compact.IndexOf(':');
        if (schemeEnd < 0)
        {
            // An entity we cannot decode could still stand for ':' - refuse it before the first path delimiter
            return !UndecodedEntityBeforeDelimiter.IsMatch(compact);
        }

        // A colon after the first path/query/fragment delimiter is not a scheme separator (e.g. "/a:b", "?x=a:b")
        var delimiter = compact.IndexOfAny(['/', '?', '#']);
        if (delimiter >= 0 && delimiter < schemeEnd)
        {
            return true;
        }

        return AllowedSchemes.Contains(compact[..schemeEnd], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Returns the URL if it is safe, otherwise null.</summary>
    public static string? OrNull(string? url) => IsSafe(url) ? url : null;
}
