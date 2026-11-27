using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Common;

namespace SharpQuill.Application.Comments.Repositories;

public class AdminCommentRepository : BaseRepository, IAdminCommentRepository
{
    public AdminCommentRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetAllAsync(PaginationParams pagination, CommentStatus? status = null)
    {
        var whereClause = status.HasValue ? "WHERE c.status = @Status" : "";

        var countSql = status.HasValue
            ? "SELECT COUNT(*) FROM comments WHERE status = @Status"
            : "SELECT COUNT(*) FROM comments";

        var countParams = status.HasValue
            ? new[] { new NpgsqlParameter("@Status", (int)status.Value) }
            : Array.Empty<NpgsqlParameter>();

        var totalCount = await ExecuteScalarIntAsync(countSql, countParams);

        var sql = $@"
            SELECT c.id, c.article_id, c.parent_comment_id, c.author_name, c.author_email,
                   c.content, c.status, c.ip_address, c.created_at, c.updated_at,
                   a.title AS article_title
            FROM comments c
            LEFT JOIN articles a ON c.article_id = a.id
            {whereClause}
            ORDER BY c.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var queryParams = new List<NpgsqlParameter>
        {
            new("@Limit", pagination.GetValidatedPageSize()),
            new("@Offset", pagination.GetOffset())
        };
        if (status.HasValue)
            queryParams.Add(new NpgsqlParameter("@Status", (int)status.Value));

        var comments = await QueryAsync(sql, MapAdminComment, queryParams.ToArray());

        return (comments, totalCount);
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT c.id, c.article_id, c.parent_comment_id, c.author_name, c.author_email,
                   c.content, c.status, c.ip_address, c.created_at, c.updated_at,
                   a.title AS article_title
            FROM comments c
            LEFT JOIN articles a ON c.article_id = a.id
            WHERE c.id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapAdminComment,
            new NpgsqlParameter("@Id", id));
    }

    public async Task UpdateStatusAsync(int id, CommentStatus status)
    {
        const string sql = @"
            UPDATE comments SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Status", (int)status);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM comments WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<(int Pending, int Approved, int Rejected)> GetCountsByStatusAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*) FILTER (WHERE status = 0) AS pending,
                COUNT(*) FILTER (WHERE status = 1) AS approved,
                COUNT(*) FILTER (WHERE status = 2) AS rejected
            FROM comments";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return (
                reader.GetInt32(reader.GetOrdinal("pending")),
                reader.GetInt32(reader.GetOrdinal("approved")),
                reader.GetInt32(reader.GetOrdinal("rejected"))
            );
        }

        return (0, 0, 0);
    }

    private static Comment MapAdminComment(NpgsqlDataReader reader)
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
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at"),
            ArticleTitle = GetNullableString(reader, "article_title")
        };
    }
}
