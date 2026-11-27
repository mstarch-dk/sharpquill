using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Likes;

namespace SharpQuill.Application.Likes.Repositories;

public class ArticleLikeRepository : BaseRepository, IArticleLikeRepository
{
    public ArticleLikeRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<bool> ArticleExistsAsync(int articleId)
    {
        const string sql = "SELECT COUNT(*) FROM public_articles WHERE id = @ArticleId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@ArticleId", articleId)) > 0;
    }

    public async Task<int> GetCountAsync(int articleId)
    {
        const string sql = "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@ArticleId", articleId));
    }

    public async Task<bool> HasVisitorLikedAsync(int articleId, string visitorId)
    {
        const string sql = "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId AND visitor_id = @VisitorId";
        var count = await ExecuteScalarIntAsync(sql,
            new NpgsqlParameter("@ArticleId", articleId),
            new NpgsqlParameter("@VisitorId", visitorId));
        return count > 0;
    }

    public async Task<bool> ToggleLikeAsync(int articleId, string visitorId, string? ipAddress)
    {
        await using var connection = await DataSource.OpenConnectionAsync();

        // Try to delete first (unlike)
        await using (var deleteCommand = new NpgsqlCommand(
            "DELETE FROM article_likes WHERE article_id = @ArticleId AND visitor_id = @VisitorId", connection))
        {
            deleteCommand.Parameters.AddWithValue("@ArticleId", articleId);
            deleteCommand.Parameters.AddWithValue("@VisitorId", visitorId);

            var deleted = await deleteCommand.ExecuteNonQueryAsync();
            if (deleted > 0)
                return false; // Was liked, now unliked
        }

        // If nothing was deleted, insert (like)
        await using var insertCommand = new NpgsqlCommand(
            "INSERT INTO article_likes (article_id, visitor_id, ip_address, created_at) VALUES (@ArticleId, @VisitorId, @IpAddress, @CreatedAt)", connection);

        insertCommand.Parameters.AddWithValue("@ArticleId", articleId);
        insertCommand.Parameters.AddWithValue("@VisitorId", visitorId);
        insertCommand.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        await insertCommand.ExecuteNonQueryAsync();
        return true; // Now liked
    }
}
