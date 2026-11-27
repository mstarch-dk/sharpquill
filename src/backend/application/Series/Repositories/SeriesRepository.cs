using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Series;

namespace SharpQuill.Application.Series.Repositories;

public class SeriesRepository : BaseRepository, ISeriesRepository
{
    public SeriesRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<ArticleSeries>> GetAllWithArticleCountAsync()
    {
        var sql = @"
            SELECT
                s.id, s.name, s.slug, s.description, s.created_at, s.updated_at, s.theme_slug,
                COUNT(a.id) AS article_count
            FROM series s
            INNER JOIN public_articles a ON s.id = a.series_id
            GROUP BY s.id, s.name, s.slug, s.description, s.created_at, s.updated_at, s.theme_slug
            ORDER BY s.name";

        return await QueryAsync(sql, MapSeriesWithCount);
    }

    public async Task<ArticleSeries?> GetBySlugAsync(string slug)
    {
        var sql = @"
            SELECT id, name, slug, description, created_at, updated_at, theme_slug
            FROM series
            WHERE slug = @slug";

        return await QuerySingleOrDefaultAsync(sql, MapSeries,
            new NpgsqlParameter("@slug", slug));
    }

    public async Task<IEnumerable<Article>> GetArticlesBySeriesIdAsync(int seriesId)
    {
        var sql = @"
            SELECT
                a.id, a.title, a.slug, ac.excerpt, a.published_at, a.series_order
            FROM public_articles a
            LEFT JOIN article_contents ac ON a.published_version_id = ac.id
            WHERE a.series_id = @seriesId
            ORDER BY a.series_order ASC";

        return await QueryAsync(sql, MapSeriesArticle,
            new NpgsqlParameter("@seriesId", seriesId));
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
            SeriesOrder = GetNullable<int>(reader, "series_order")
        };
    }
}

public class SeriesWithCount : ArticleSeries
{
    public int ArticleCount { get; set; }
}
