using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Comments;

namespace SharpQuill.Application.Comments.Repositories;

public class CommentRepository : BaseRepository, ICommentRepository
{
    public CommentRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<Comment>> GetApprovedByArticleIdAsync(int articleId)
    {
        const string sql = @"
            SELECT id, article_id, parent_comment_id, author_name, author_email,
                   content, status, ip_address, created_at, updated_at
            FROM comments
            WHERE article_id = @ArticleId AND status = @Status
            ORDER BY created_at ASC";

        return await QueryAsync(sql, MapComment,
            new NpgsqlParameter("@ArticleId", articleId),
            new NpgsqlParameter("@Status", (int)CommentStatus.Approved));
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        const string sql = @"
            INSERT INTO comments (article_id, parent_comment_id, author_name, author_email, content, status, ip_address, created_at)
            VALUES (@ArticleId, @ParentCommentId, @AuthorName, @AuthorEmail, @Content, @Status, @IpAddress, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@ArticleId", comment.ArticleId);
        command.Parameters.AddWithValue("@ParentCommentId", (object?)comment.ParentCommentId ?? DBNull.Value);
        command.Parameters.AddWithValue("@AuthorName", comment.AuthorName);
        command.Parameters.AddWithValue("@AuthorEmail", comment.AuthorEmail);
        command.Parameters.AddWithValue("@Content", comment.Content);
        command.Parameters.AddWithValue("@Status", (int)comment.Status);
        command.Parameters.AddWithValue("@IpAddress", (object?)comment.IpAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", comment.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        comment.Id = Convert.ToInt32(id);

        return comment;
    }

    public async Task<bool> ArticleExistsAsync(int articleId)
    {
        const string sql = "SELECT COUNT(*) FROM public_articles WHERE id = @ArticleId";
        var count = await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@ArticleId", articleId));
        return count > 0;
    }

    public async Task<bool> CommentsEnabledAsync(int articleId)
    {
        const string sql = "SELECT comments_enabled FROM articles WHERE id = @ArticleId";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ArticleId", articleId);

        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    public async Task<bool> RequiresSignInAsync(int articleId)
    {
        const string sql = "SELECT requires_sign_in FROM articles WHERE id = @ArticleId";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ArticleId", articleId);

        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, article_id, parent_comment_id, author_name, author_email,
                   content, status, ip_address, created_at, updated_at
            FROM comments
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapComment,
            new NpgsqlParameter("@Id", id));
    }

    private static Comment MapComment(NpgsqlDataReader reader)
    {
        return new Comment
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            ArticleId = reader.GetInt32(reader.GetOrdinal("article_id")),
            ParentCommentId = GetNullable<int>(reader, "parent_comment_id"),
            AuthorName = reader.GetString(reader.GetOrdinal("author_name")),
            AuthorEmail = reader.GetString(reader.GetOrdinal("author_email")),
            Content = reader.GetString(reader.GetOrdinal("content")),
            Status = (CommentStatus)reader.GetInt32(reader.GetOrdinal("status")),
            IpAddress = GetNullableString(reader, "ip_address"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at")
        };
    }
}
