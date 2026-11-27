namespace SharpQuill.Application.Articles.DTOs;

/// <summary>One search match: the article (as in article lists) and a snippet of its text around the matches.</summary>
public record SearchResultDto(
    ArticleListDto Article,
    IReadOnlyList<SearchSnippetPartDto> Snippet
);

/// <summary>
/// A run of snippet text. <see cref="Match"/> marks the words that matched the query, so clients can highlight them
/// without rendering HTML from the API.
/// </summary>
public record SearchSnippetPartDto(
    string Text,
    bool Match
);
