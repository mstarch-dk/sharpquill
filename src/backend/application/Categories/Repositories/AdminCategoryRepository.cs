using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Categories;

namespace SharpQuill.Application.Categories.Repositories;

public class AdminCategoryRepository : BaseRepository, IAdminCategoryRepository
{
    public AdminCategoryRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, name, slug, description, created_at
            FROM categories
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapCategory, new NpgsqlParameter("@Id", id));
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, name, slug, description, created_at
            FROM categories
            ORDER BY name";

        return await QueryAsync(sql, MapCategory);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        const string sql = @"
            INSERT INTO categories (name, slug, description, created_at)
            VALUES (@Name, @Slug, @Description, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Name", category.Name);
        command.Parameters.AddWithValue("@Slug", category.Slug);
        command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", category.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        category.Id = Convert.ToInt32(id);

        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        const string sql = @"
            UPDATE categories
            SET name = @Name, slug = @Slug, description = @Description
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", category.Id);
        command.Parameters.AddWithValue("@Name", category.Name);
        command.Parameters.AddWithValue("@Slug", category.Slug);
        command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return category;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM categories WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM categories WHERE slug = @Slug AND id != @ExcludeId"
            : "SELECT COUNT(*) FROM categories WHERE slug = @Slug";

        var parameters = excludeId.HasValue
            ? new[] { new NpgsqlParameter("@Slug", slug), new NpgsqlParameter("@ExcludeId", excludeId.Value) }
            : new[] { new NpgsqlParameter("@Slug", slug) };

        var count = await ExecuteScalarIntAsync(sql, parameters);
        return count > 0;
    }

    public async Task<int> GetArticleCountAsync(int categoryId)
    {
        const string sql = "SELECT COUNT(*) FROM articles WHERE category_id = @CategoryId";
        return await ExecuteScalarIntAsync(sql, new NpgsqlParameter("@CategoryId", categoryId));
    }

    private static Category MapCategory(NpgsqlDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            Description = GetNullableString(reader, "description"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
