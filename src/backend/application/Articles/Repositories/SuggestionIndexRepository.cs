using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Articles.Suggestions;

namespace SharpQuill.Application.Articles.Repositories;

/// <summary>
/// Loads the search-suggestion index (see <see cref="SuggestionIndex"/>). Reads the articles table, not the
/// public_articles view: the index also serves the admin, and checks public visibility itself when it answers.
/// </summary>
public class SuggestionIndexRepository : BaseRepository, ISuggestionIndexRepository
{
    private const string ArticleSelectSql = @"
        SELECT a.id, a.title, a.slug, a.content_type, a.status, a.published_at, a.requires_sign_in,
            a.category_id, a.series_id,
            coalesce((SELECT array_agg(at.tag_id ORDER BY at.tag_id) FROM article_tags at WHERE at.article_id = a.id),
                '{}') AS tag_ids
        FROM articles a";

    public SuggestionIndexRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<SuggestionData> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        // One snapshot for all four reads, so an article never refers to a tag the load has not seen yet.
        await using var transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead, cancellationToken);

        var articles = await ReadAsync(connection, ArticleSelectSql, MapArticle, cancellationToken);
        var tags = await ReadAsync(connection, TermSelectSql(SuggestionKind.Tag), MapTerm, cancellationToken);
        var categories = await ReadAsync(connection, TermSelectSql(SuggestionKind.Category), MapTerm, cancellationToken);
        var series = await ReadAsync(connection, TermSelectSql(SuggestionKind.Series), MapTerm, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new SuggestionData(articles, tags, categories, series);
    }

    public async Task<IReadOnlyList<SuggestionArticle>> LoadArticlesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, $"{ArticleSelectSql} WHERE a.id = ANY(@ids)", MapArticle, cancellationToken,
            new NpgsqlParameter("@ids", ids.ToArray()));
    }

    public async Task<IReadOnlyList<SuggestionTerm>> LoadTermsAsync(
        SuggestionKind kind, IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, $"{TermSelectSql(kind)} WHERE id = ANY(@ids)", MapTerm, cancellationToken,
            new NpgsqlParameter("@ids", ids.ToArray()));
    }

    private static string TermSelectSql(SuggestionKind kind) => kind switch
    {
        SuggestionKind.Tag => "SELECT id, name, slug FROM tags",
        SuggestionKind.Category => "SELECT id, name, slug FROM categories",
        SuggestionKind.Series => "SELECT id, name, slug FROM series",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a term kind")
    };

    private static async Task<List<T>> ReadAsync<T>(
        NpgsqlConnection connection, string sql, Func<NpgsqlDataReader, T> map, CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(map(reader));
        }
        return results;
    }

    private static SuggestionArticle MapArticle(NpgsqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("id")),
        reader.GetString(reader.GetOrdinal("title")),
        reader.GetString(reader.GetOrdinal("slug")),
        (ContentType)reader.GetInt32(reader.GetOrdinal("content_type")),
        (ArticleStatus)reader.GetInt32(reader.GetOrdinal("status")),
        GetNullable<DateTime>(reader, "published_at"),
        reader.GetBoolean(reader.GetOrdinal("requires_sign_in")),
        GetNullable<int>(reader, "category_id"),
        GetNullable<int>(reader, "series_id"),
        reader.GetFieldValue<int[]>(reader.GetOrdinal("tag_ids")));

    private static SuggestionTerm MapTerm(NpgsqlDataReader reader) => new(
        reader.GetInt32(reader.GetOrdinal("id")),
        reader.GetString(reader.GetOrdinal("name")),
        reader.GetString(reader.GetOrdinal("slug")));
}
