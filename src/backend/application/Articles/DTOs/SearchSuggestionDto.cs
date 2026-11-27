using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.DTOs;

/// <summary>
/// A public search suggestion. <see cref="Type"/>: article, tag, category or series; <see cref="Url"/>: the website
/// path it links to.
/// </summary>
public record SearchSuggestionDto(string Type, string Title, string Url);

/// <summary>An admin search suggestion: an article in any status.</summary>
public record ArticleSuggestionDto(int Id, string Title, ArticleStatus Status, DateTime? PublishedAt);
