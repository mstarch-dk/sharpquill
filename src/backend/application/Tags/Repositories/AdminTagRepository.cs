using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Tags;

namespace SharpQuill.Application.Tags.Repositories;

public class AdminTagRepository : BaseRepository, IAdminTagRepository
{
    public AdminTagRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, name, slug, created_at
            FROM tags
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapTag, new NpgsqlParameter("@Id", id));
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, name, slug, created_at
            FROM tags
            ORDER BY name";

        return await QueryAsync(sql, MapTag);
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        const string sql = @"
            INSERT INTO tags (name, slug, created_at)
            VALUES (@Name, @Slug, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Name", tag.Name);
        command.Parameters.AddWithValue("@Slug", tag.Slug);
        command.Parameters.AddWithValue("@CreatedAt", tag.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        tag.Id = Convert.ToInt32(id);

        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        const string sql = @"
            UPDATE tags
            SET name = @Name, slug = @Slug
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", tag.Id);
        command.Parameters.AddWithValue("@Name", tag.Name);
        command.Parameters.AddWithValue("@Slug", tag.Slug);

        await command.ExecuteNonQueryAsync();

        return tag;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM tags WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM tags WHERE slug = @Slug AND id != @ExcludeId"
            : "SELECT COUNT(*) FROM tags WHERE slug = @Slug";

        var parameters = excludeId.HasValue
            ? new[] { new NpgsqlParameter("@Slug", slug), new NpgsqlParameter("@ExcludeId", excludeId.Value) }
            : new[] { new NpgsqlParameter("@Slug", slug) };

        var count = await ExecuteScalarIntAsync(sql, parameters);
        return count > 0;
    }

    public async Task<int> GetArticleCountAsync(int tagId)
    {
        const string sql = "SELECT COUNT(*) FROM article_tags WHERE tag_id = @TagId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@TagId", tagId));
    }

    private static Tag MapTag(NpgsqlDataReader reader)
    {
        return new Tag
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
