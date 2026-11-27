using SharpQuill.Core.Articles.Suggestions;

namespace SharpQuill.Application.Articles.Suggestions;

/// <summary>
/// The current search-suggestion index (singleton). The API's SuggestionIndexListener loads it at startup and swaps
/// in a new index after every change; readers take <see cref="Current"/> once per request, without locks.
/// </summary>
public class SuggestionIndexHolder
{
    private volatile SuggestionIndex _current = SuggestionIndex.Empty;
    private volatile bool _isLoaded;

    public SuggestionIndex Current => _current;

    /// <summary>False until the first full load has finished; until then every query answers nothing.</summary>
    public bool IsLoaded => _isLoaded;

    public void Replace(SuggestionIndex index)
    {
        _current = index;
        _isLoaded = true;
    }
}
