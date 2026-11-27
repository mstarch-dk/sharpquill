using System.Text;
using System.Text.RegularExpressions;

namespace SharpQuill.Core.Articles.Suggestions;

/// <summary>Turns titles, names and typed queries into the words the suggestion index compares.</summary>
public static partial class SuggestionText
{
    /// <summary>A query needs at least this many letters or digits before it is answered.</summary>
    public const int MinQueryLength = 2;

    /// <summary>Further query words are ignored.</summary>
    public const int MaxQueryTokens = 5;

    /// <summary>Longer input is cut to this many characters before it is split into words.</summary>
    public const int MaxQueryLength = 100;

    /// <summary>
    /// Lower-cased (invariant culture) NFC words: runs of letters, digits and combining marks. Diacritics are kept, as
    /// æ, ø and å are letters of their own in Danish and must not match a or o.
    /// </summary>
    public static string[] Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return WordRegex().Matches(normalized).Select(m => m.Value).ToArray();
    }

    /// <summary>
    /// The distinct words of a query, at most <see cref="MaxQueryTokens"/>, or an empty array when the query has fewer
    /// than <see cref="MinQueryLength"/> letters and digits.
    /// </summary>
    public static string[] TokenizeQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return [];
        }

        var tokens = Tokenize(query.Length > MaxQueryLength ? query[..MaxQueryLength] : query);
        if (tokens.Sum(t => t.Length) < MinQueryLength)
        {
            return [];
        }

        return tokens.Distinct(StringComparer.Ordinal).Take(MaxQueryTokens).ToArray();
    }

    [GeneratedRegex(@"[\p{L}\p{N}\p{M}]+")]
    private static partial Regex WordRegex();
}
