using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Repositories;

public class TitleAutocompleteRepository : BaseRepository, ITitleAutocompleteRepository
{
    public TitleAutocompleteRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<List<TitleAutocompleteResult>> SearchAsync(string query, int limit = 10)
    {
        const string sql = @"
            SELECT id, title, slug, content_type
            FROM articles
            WHERE status = 1
              AND title ILIKE @Query
            ORDER BY
                CASE WHEN title ILIKE @PrefixQuery THEN 0 ELSE 1 END,
                title
            LIMIT @Limit";

        var escaped = EscapeLike(query);
        var pattern = $"%{escaped}%";
        var prefixPattern = $"{escaped}%";

        return await QueryAsync(sql, MapResult,
            new NpgsqlParameter("@Query", pattern),
            new NpgsqlParameter("@PrefixQuery", prefixPattern),
            new NpgsqlParameter("@Limit", limit));
    }

    /// <summary>Escapes LIKE wildcards (and the default escape character) so user input matches literally.</summary>
    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static TitleAutocompleteResult MapResult(NpgsqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        var title = reader.GetString(reader.GetOrdinal("title"));
        var slug = reader.GetString(reader.GetOrdinal("slug"));
        var contentType = (ContentType)reader.GetInt32(reader.GetOrdinal("content_type"));
        var url = contentType == ContentType.Article ? $"/blog/{slug}" : $"/{slug}";

        return new TitleAutocompleteResult(id, title, slug, contentType, url);
    }
}
