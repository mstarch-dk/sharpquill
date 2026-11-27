namespace SharpQuill.Core.Articles;

public interface ITitleAutocompleteRepository
{
    Task<List<TitleAutocompleteResult>> SearchAsync(string query, int limit = 10);
}

public record TitleAutocompleteResult(
    int Id,
    string Title,
    string Slug,
    ContentType ContentType,
    string Url
);
