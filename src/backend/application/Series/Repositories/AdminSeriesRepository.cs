using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Series;

namespace SharpQuill.Application.Series.Repositories;

public class AdminSeriesRepository : BaseRepository, IAdminSeriesRepository
{
    public AdminSeriesRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<ArticleSeries?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, name, slug, description, created_at, updated_at, theme_slug
            FROM series
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapSeries, new NpgsqlParameter("@Id", id));
    }

    public async Task<IEnumerable<ArticleSeries>> GetAllAsync()
    {
        const string sql = @"
            SELECT
                s.id, s.name, s.slug, s.description, s.created_at, s.updated_at, s.theme_slug,
                COUNT(a.id) AS article_count
            FROM series s
            LEFT JOIN articles a ON s.id = a.series_id
            GROUP BY s.id, s.name, s.slug, s.description, s.created_at, s.updated_at, s.theme_slug
            ORDER BY s.name";

        return await QueryAsync(sql, MapSeriesWithCount);
    }

    public async Task<ArticleSeries> CreateAsync(ArticleSeries series)
    {
        const string sql = @"
            INSERT INTO series (name, slug, description, theme_slug, created_at)
            VALUES (@Name, @Slug, @Description, @ThemeSlug, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Name", series.Name);
        command.Parameters.AddWithValue("@Slug", series.Slug);
        command.Parameters.AddWithValue("@Description", (object?)series.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@ThemeSlug", (object?)series.ThemeSlug ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", series.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        series.Id = Convert.ToInt32(id);

        return series;
    }

    public async Task<ArticleSeries> UpdateAsync(ArticleSeries series)
    {
        const string sql = @"
            UPDATE series
            SET name = @Name, slug = @Slug, description = @Description, theme_slug = @ThemeSlug, updated_at = @UpdatedAt
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", series.Id);
        command.Parameters.AddWithValue("@Name", series.Name);
        command.Parameters.AddWithValue("@Slug", series.Slug);
        command.Parameters.AddWithValue("@Description", (object?)series.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@ThemeSlug", (object?)series.ThemeSlug ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", series.UpdatedAt ?? DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();

        return series;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = await DataSource.OpenConnectionAsync();

        // First NULL out series_id/series_order on all articles in this series
        await using (var clearCommand = new NpgsqlCommand(
            "UPDATE articles SET series_id = NULL, series_order = NULL WHERE series_id = @Id", connection))
        {
            clearCommand.Parameters.AddWithValue("@Id", id);
            await clearCommand.ExecuteNonQueryAsync();
        }

        // Then delete the series row
        await using var deleteCommand = new NpgsqlCommand("DELETE FROM series WHERE id = @Id", connection);
        deleteCommand.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM series WHERE slug = @Slug AND id != @ExcludeId"
            : "SELECT COUNT(*) FROM series WHERE slug = @Slug";

        var parameters = excludeId.HasValue
            ? new[] { new NpgsqlParameter("@Slug", slug), new NpgsqlParameter("@ExcludeId", excludeId.Value) }
            : new[] { new NpgsqlParameter("@Slug", slug) };

        var count = await ExecuteScalarIntAsync(sql, parameters);
        return count > 0;
    }

    public async Task<int> GetArticleCountAsync(int seriesId)
    {
        const string sql = "SELECT COUNT(*) FROM articles WHERE series_id = @SeriesId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@SeriesId", seriesId));
    }

    public async Task<IEnumerable<Article>> GetArticlesInSeriesAsync(int seriesId)
    {
        const string sql = @"
            SELECT
                a.id, a.title, a.slug, lv.excerpt, a.published_at,
                a.status, a.series_order,
                au.name AS author_name
            FROM articles a
            INNER JOIN authors au ON a.author_id = au.id
            LEFT JOIN LATERAL (
                SELECT ac.excerpt
                FROM article_contents ac
                WHERE ac.article_id = a.id
                ORDER BY ac.version_number DESC
                LIMIT 1
            ) lv ON true
            WHERE a.series_id = @SeriesId
            ORDER BY a.series_order ASC";

        return await QueryAsync(sql, MapSeriesArticle,
            new NpgsqlParameter("@SeriesId", seriesId));
    }

    public async Task SetArticleSeriesAsync(int articleId, int seriesId, int order)
    {
        const string sql = @"
            UPDATE articles
            SET series_id = @SeriesId, series_order = @Order
            WHERE id = @ArticleId";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@ArticleId", articleId);
        command.Parameters.AddWithValue("@SeriesId", seriesId);
        command.Parameters.AddWithValue("@Order", order);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveArticleFromSeriesAsync(int articleId)
    {
        const string sql = @"
            UPDATE articles
            SET series_id = NULL, series_order = NULL
            WHERE id = @ArticleId";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@ArticleId", articleId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ReorderArticlesAsync(int seriesId, IReadOnlyList<int> articleIds)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        for (int i = 0; i < articleIds.Count; i++)
        {
            await using var command = new NpgsqlCommand(
                "UPDATE articles SET series_order = @Order WHERE id = @ArticleId AND series_id = @SeriesId",
                connection, transaction);

            command.Parameters.AddWithValue("@Order", i + 1);
            command.Parameters.AddWithValue("@ArticleId", articleIds[i]);
            command.Parameters.AddWithValue("@SeriesId", seriesId);

            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private static ArticleSeries MapSeries(NpgsqlDataReader reader)
    {
        return new ArticleSeries
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            Description = GetNullableString(reader, "description"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at"),
            ThemeSlug = GetNullableString(reader, "theme_slug")
        };
    }

    private static SeriesWithCount MapSeriesWithCount(NpgsqlDataReader reader)
    {
        return new SeriesWithCount
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            Description = GetNullableString(reader, "description"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at"),
            ThemeSlug = GetNullableString(reader, "theme_slug"),
            ArticleCount = reader.GetInt32(reader.GetOrdinal("article_count"))
        };
    }

    private static Article MapSeriesArticle(NpgsqlDataReader reader)
    {
        return new Article
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            Excerpt = GetNullableString(reader, "excerpt"),
            PublishedAt = GetNullable<DateTime>(reader, "published_at"),
            Status = (ArticleStatus)reader.GetInt32(reader.GetOrdinal("status")),
            SeriesOrder = GetNullable<int>(reader, "series_order"),
            Author = new Author
            {
                Name = reader.GetString(reader.GetOrdinal("author_name"))
            }
        };
    }
}
