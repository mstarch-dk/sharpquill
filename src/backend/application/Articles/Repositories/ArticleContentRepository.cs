using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;

namespace SharpQuill.Application.Articles.Repositories;

public class ArticleContentRepository : BaseRepository, IArticleContentRepository
{
    public ArticleContentRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<ArticleContent?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, article_id, content, excerpt, meta_title, meta_description, layout, page_style, featured_image_size, version_number, created_at
            FROM article_contents
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapArticleContent, new NpgsqlParameter("@Id", id));
    }

    public async Task<ArticleContent?> GetLatestVersionAsync(int articleId)
    {
        const string sql = @"
            SELECT id, article_id, content, excerpt, meta_title, meta_description, layout, page_style, featured_image_size, version_number, created_at
            FROM article_contents
            WHERE article_id = @ArticleId
            ORDER BY version_number DESC
            LIMIT 1";

        return await QuerySingleOrDefaultAsync(sql, MapArticleContent, new NpgsqlParameter("@ArticleId", articleId));
    }

    public async Task<IEnumerable<ArticleContent>> GetVersionsByArticleIdAsync(int articleId)
    {
        const string sql = @"
            SELECT id, article_id, content, excerpt, meta_title, meta_description, layout, page_style, featured_image_size, version_number, created_at
            FROM article_contents
            WHERE article_id = @ArticleId
            ORDER BY version_number DESC";

        return await QueryAsync(sql, MapArticleContent, new NpgsqlParameter("@ArticleId", articleId));
    }

    public async Task<ArticleContent> CreateVersionAsync(ArticleContent version)
    {
        const string sql = @"
            INSERT INTO article_contents (article_id, content, excerpt, meta_title, meta_description, layout, page_style, featured_image_size, version_number, created_at)
            VALUES (@ArticleId, @Content, @Excerpt, @MetaTitle, @MetaDescription, @Layout, @PageStyle, @FeaturedImageSize, @VersionNumber, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@ArticleId", version.ArticleId);
        command.Parameters.AddWithValue("@Content", (object?)version.Content ?? DBNull.Value);
        command.Parameters.AddWithValue("@Excerpt", (object?)version.Excerpt ?? DBNull.Value);
        command.Parameters.AddWithValue("@MetaTitle", (object?)version.MetaTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("@MetaDescription", (object?)version.MetaDescription ?? DBNull.Value);
        command.Parameters.Add(new NpgsqlParameter("@Layout", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = (object?)version.Layout ?? DBNull.Value });
        command.Parameters.AddWithValue("@PageStyle", (object?)version.PageStyle ?? DBNull.Value);
        command.Parameters.AddWithValue("@FeaturedImageSize", (object?)version.FeaturedImageSize ?? DBNull.Value);
        command.Parameters.AddWithValue("@VersionNumber", version.VersionNumber);
        command.Parameters.AddWithValue("@CreatedAt", version.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        version.Id = Convert.ToInt32(id);

        return version;
    }

    public async Task<int> GetNextVersionNumberAsync(int articleId)
    {
        const string sql = "SELECT COALESCE(MAX(version_number), 0) + 1 FROM article_contents WHERE article_id = @ArticleId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@ArticleId", articleId));
    }

    private static ArticleContent MapArticleContent(NpgsqlDataReader reader)
    {
        return new ArticleContent
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            ArticleId = reader.GetInt32(reader.GetOrdinal("article_id")),
            Content = GetNullableString(reader, "content"),
            Excerpt = GetNullableString(reader, "excerpt"),
            MetaTitle = GetNullableString(reader, "meta_title"),
            MetaDescription = GetNullableString(reader, "meta_description"),
            Layout = GetNullableString(reader, "layout"),
            PageStyle = GetNullableString(reader, "page_style"),
            FeaturedImageSize = GetNullableString(reader, "featured_image_size"),
            VersionNumber = reader.GetInt32(reader.GetOrdinal("version_number")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
