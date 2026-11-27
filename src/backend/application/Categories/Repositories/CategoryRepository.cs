using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Categories;

namespace SharpQuill.Application.Categories.Repositories;

public class CategoryRepository : BaseRepository, ICategoryRepository
{
    public CategoryRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<Category>> GetAllWithArticleCountAsync()
    {
        var sql = @"
            SELECT
                c.id, c.name, c.slug, c.description, c.created_at,
                COUNT(a.id) AS article_count
            FROM categories c
            LEFT JOIN public_articles a ON c.id = a.category_id
            GROUP BY c.id, c.name, c.slug, c.description, c.created_at
            ORDER BY c.name";

        return await QueryAsync(sql, MapCategoryWithCount);
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        var sql = @"
            SELECT id, name, slug, description, created_at
            FROM categories
            WHERE slug = @slug";

        return await QuerySingleOrDefaultAsync(sql, MapCategory,
            new NpgsqlParameter("@slug", slug));
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

    private static CategoryWithCount MapCategoryWithCount(NpgsqlDataReader reader)
    {
        return new CategoryWithCount
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            Description = GetNullableString(reader, "description"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            ArticleCount = reader.GetInt32(reader.GetOrdinal("article_count"))
        };
    }
}

public class CategoryWithCount : Category
{
    public int ArticleCount { get; set; }
}
