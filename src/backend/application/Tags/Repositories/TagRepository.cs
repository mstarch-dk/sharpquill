using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Tags;

namespace SharpQuill.Application.Tags.Repositories;

public class TagRepository : BaseRepository, ITagRepository
{
    public TagRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<Tag>> GetAllWithArticleCountAsync()
    {
        var sql = @"
            SELECT
                t.id, t.name, t.slug, t.created_at,
                COUNT(a.id) AS article_count
            FROM tags t
            LEFT JOIN article_tags at ON t.id = at.tag_id
            LEFT JOIN public_articles a ON at.article_id = a.id
            GROUP BY t.id, t.name, t.slug, t.created_at
            ORDER BY t.name";

        return await QueryAsync(sql, MapTagWithCount);
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        var sql = @"
            SELECT id, name, slug, created_at
            FROM tags
            WHERE slug = @slug";

        return await QuerySingleOrDefaultAsync(sql, MapTag,
            new NpgsqlParameter("@slug", slug));
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

    private static TagWithCount MapTagWithCount(NpgsqlDataReader reader)
    {
        return new TagWithCount
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            ArticleCount = reader.GetInt32(reader.GetOrdinal("article_count"))
        };
    }
}

public class TagWithCount : Tag
{
    public int ArticleCount { get; set; }
}
