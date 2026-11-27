using SharpQuill.Api.Tests.Fixtures;
using Npgsql;

namespace SharpQuill.Api.Tests.Helpers;

/// <summary>Direct SQL access to the test database, for arranging state and checking what an endpoint stored.</summary>
public static class TestSql
{
    public static async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Create(sql, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<T> ScalarAsync<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Create(sql, parameters);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>Inserts a comment and returns its id. Status: 0 = pending, 1 = approved, 2 = rejected.</summary>
    public static Task<int> InsertCommentAsync(int articleId, string content, int status,
        int? parentId = null, string authorName = "Seeded Reader", DateTime? createdAt = null) =>
        ScalarAsync<int>(@"
            INSERT INTO comments (article_id, parent_comment_id, author_name, author_email, content, status, ip_address, created_at)
            VALUES (@ArticleId, @ParentId, @AuthorName, 'seeded@example.com', @Content, @Status, '192.0.2.1', @CreatedAt)
            RETURNING id",
            ("ArticleId", articleId), ("ParentId", parentId), ("AuthorName", authorName),
            ("Content", content), ("Status", status), ("CreatedAt", createdAt ?? DateTime.UtcNow));

    /// <summary>
    /// Runs the action with a site setting set to the given value, then restores the previous value
    /// (or removes the row if it did not exist).
    /// </summary>
    public static async Task<T> WithSiteSettingAsync<T>(string key, string value, Func<Task<T>> action)
    {
        await using var readCmd = Create("SELECT value FROM site_settings WHERE key = @Key", ("Key", key));
        var previous = await readCmd.ExecuteScalarAsync();

        await UpsertSettingAsync(key, value);
        try
        {
            return await action();
        }
        finally
        {
            if (previous is string previousValue)
                await UpsertSettingAsync(key, previousValue);
            else
                await ExecuteAsync("DELETE FROM site_settings WHERE key = @Key", ("Key", key));
        }
    }

    private static Task UpsertSettingAsync(string key, string value) =>
        ExecuteAsync(@"
            INSERT INTO site_settings (key, value, updated_at) VALUES (@Key, @Value, NOW())
            ON CONFLICT (key) DO UPDATE SET value = @Value, updated_at = NOW()",
            ("Key", key), ("Value", value));

    private static NpgsqlCommand Create(string sql, params (string Name, object? Value)[] parameters)
    {
        var cmd = TestDatabase.DataSource.CreateCommand(sql);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return cmd;
    }
}
