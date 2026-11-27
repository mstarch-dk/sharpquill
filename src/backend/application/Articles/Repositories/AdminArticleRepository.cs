using System.Text.Json;
using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Common;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;
using SharpQuill.Core.Tags;

namespace SharpQuill.Application.Articles.Repositories;

public class AdminArticleRepository : BaseRepository, IAdminArticleRepository
{
    public AdminArticleRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    private const string BaseSelectSql = @"
        SELECT
            a.id, a.title, a.slug, a.content_type, a.comments_enabled, a.requires_sign_in,
            a.published_at, a.created_at, a.updated_at, a.status, a.author_id, a.category_id,
            a.series_id, a.series_order, a.published_version_id, a.author_name AS article_author_name,
            a.featured_image_id, mi.path AS featured_image_path,
            mi.thumbnail_path AS featured_image_thumbnail_path, mi.medium_path AS featured_image_medium_path,
            au.id as author_id_nav, au.email as author_email, au.name as linked_author_name,
            au.bio as author_bio, au.avatar_url as author_avatar,
            c.id as cat_id, c.name as cat_name, c.slug as cat_slug, c.description as cat_desc,
            s.id as series_id_nav, s.name as series_name, s.slug as series_slug,
            lv.content, lv.excerpt, lv.meta_title, lv.meta_description, lv.layout, lv.page_style, lv.featured_image_size,
            lv.version_number as current_version_number,
            COALESCE(vc.total_versions, 0) as total_versions,
            COALESCE(
                json_agg(
                    json_build_object('id', t.id, 'name', t.name, 'slug', t.slug)
                ) FILTER (WHERE t.id IS NOT NULL),
                '[]'
            ) as tags
        FROM articles a
        LEFT JOIN authors au ON a.author_id = au.id
        LEFT JOIN categories c ON a.category_id = c.id
        LEFT JOIN series s ON a.series_id = s.id
        LEFT JOIN media mi ON a.featured_image_id = mi.id
        LEFT JOIN LATERAL (
            SELECT ac.content, ac.excerpt, ac.meta_title, ac.meta_description, ac.layout, ac.page_style, ac.featured_image_size, ac.version_number
            FROM article_contents ac
            WHERE ac.article_id = a.id
            ORDER BY ac.version_number DESC
            LIMIT 1
        ) lv ON true
        LEFT JOIN LATERAL (
            SELECT COUNT(*)::int as total_versions
            FROM article_contents ac2
            WHERE ac2.article_id = a.id
        ) vc ON true
        LEFT JOIN article_tags at ON a.id = at.article_id
        LEFT JOIN tags t ON at.tag_id = t.id";

    public async Task<Article?> GetByIdAsync(int id)
    {
        var sql = $@"{BaseSelectSql}
            WHERE a.id = @Id
            GROUP BY a.id, au.id, c.id, s.id, mi.id, lv.content, lv.excerpt, lv.meta_title, lv.meta_description, lv.layout, lv.page_style, lv.featured_image_size, lv.version_number, vc.total_versions";

        return await QuerySingleOrDefaultAsync(sql, MapArticle, new NpgsqlParameter("@Id", id));
    }

    public async Task<(IEnumerable<Article> Articles, int TotalCount)> GetAllAsync(
        PaginationParams pagination, ContentType? contentType = null, ArticleAdminFilter? filter = null)
    {
        filter ??= new ArticleAdminFilter();
        var search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim();
        if (search is { Length: > ArticleAdminFilter.MaxSearchLength })
        {
            search = search[..ArticleAdminFilter.MaxSearchLength];
        }

        var conditions = new List<string>();
        var filterParameters = new List<(string Name, object Value)>();

        if (contentType.HasValue)
        {
            conditions.Add("a.content_type = @contentType");
            filterParameters.Add(("@contentType", (int)contentType.Value));
        }

        // Text: full-text match on the latest version (admin_search_vector, see migration AddAdminArticleSearch), or the
        // text anywhere in the title so a half-typed word still finds the article.
        if (search != null)
        {
            conditions.Add(@"(a.admin_search_vector @@ q.tsq OR a.title ILIKE @titleLike)");
            filterParameters.Add(("@search", search));
            filterParameters.Add(("@titleLike", $"%{EscapeLike(search)}%"));
        }

        if (filter.Status.HasValue)
        {
            conditions.Add(filter.Status.Value switch
            {
                ArticleAdminStatusFilter.Draft => $"a.status = {(int)ArticleStatus.Draft}",
                ArticleAdminStatusFilter.Published =>
                    $"a.status = {(int)ArticleStatus.Published} AND (a.published_at IS NULL OR a.published_at <= now())",
                ArticleAdminStatusFilter.Scheduled =>
                    $"a.status = {(int)ArticleStatus.Published} AND a.published_at > now()",
                ArticleAdminStatusFilter.Archived => $"a.status = {(int)ArticleStatus.Archived}",
                _ => throw new ArgumentOutOfRangeException(nameof(filter), filter.Status, "Unknown status filter")
            });
        }

        if (filter.CategoryId.HasValue)
        {
            conditions.Add("a.category_id = @categoryId");
            filterParameters.Add(("@categoryId", filter.CategoryId.Value));
        }

        if (filter.AuthorId.HasValue)
        {
            conditions.Add("a.author_id = @authorId");
            filterParameters.Add(("@authorId", filter.AuthorId.Value));
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var queryCte = search != null
            ? "WITH q AS (SELECT websearch_to_tsquery(cms_search_config(), @search) AS tsq)"
            : "WITH q AS (SELECT NULL::tsquery AS tsq)";

        NpgsqlParameter[] Parameters() => filterParameters.Select(p => new NpgsqlParameter(p.Name, p.Value)).ToArray();

        var totalCount = await ExecuteScalarIntAsync(
            $"{queryCte} SELECT COUNT(*) FROM articles a CROSS JOIN q {where}", Parameters());
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var sort = filter.Sort ?? (search != null ? ArticleAdminSort.Relevance : ArticleAdminSort.Newest);
        var orderBy = sort switch
        {
            // A title that contains the text outranks body matches; ts_rank stays below 1 for typical documents.
            ArticleAdminSort.Relevance when search != null =>
                "ts_rank(a.admin_search_vector, q.tsq) + CASE WHEN a.title ILIKE @titleLike THEN 1 ELSE 0 END DESC, a.created_at DESC, a.id DESC",
            ArticleAdminSort.Relevance or ArticleAdminSort.Newest => "a.created_at DESC, a.id DESC",
            ArticleAdminSort.Oldest => "a.created_at ASC, a.id ASC",
            ArticleAdminSort.Updated => "COALESCE(a.updated_at, a.created_at) DESC, a.id DESC",
            ArticleAdminSort.Title => "lower(a.title) ASC, a.id ASC",
            _ => throw new ArgumentOutOfRangeException(nameof(filter), sort, "Unknown sort")
        };

        // Filter, order and page on the bare articles table first; the wide select (tags, latest version) runs only
        // for the rows of the page and keeps their position.
        var sql = $@"{queryCte},
        page_ids AS (
            SELECT a.id, row_number() OVER (ORDER BY {orderBy}) AS position
            FROM articles a CROSS JOIN q
            {where}
            ORDER BY position
            LIMIT @Limit OFFSET @Offset
        )
        SELECT b.* FROM ({BaseSelectSql}
            WHERE a.id IN (SELECT id FROM page_ids)
            GROUP BY a.id, au.id, c.id, s.id, mi.id, lv.content, lv.excerpt, lv.meta_title, lv.meta_description, lv.layout, lv.page_style, lv.featured_image_size, lv.version_number, vc.total_versions
        ) AS b
        JOIN page_ids p ON p.id = b.id
        ORDER BY p.position";

        var articles = await QueryAsync(sql, MapArticle, [
            .. Parameters(),
            new NpgsqlParameter("@Limit", pagination.GetValidatedPageSize()),
            new NpgsqlParameter("@Offset", pagination.GetOffset())]);

        return (articles, totalCount);
    }

    /// <summary>Escapes the LIKE wildcards (and the escape character) so the text matches literally.</summary>
    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public async Task<Article> CreateAsync(Article article)
    {
        const string sql = @"
            INSERT INTO articles (title, slug, published_version_id,
                                  published_at, created_at, status, content_type, author_id, category_id, featured_image_id, comments_enabled, requires_sign_in, author_name)
            VALUES (@Title, @Slug, @PublishedVersionId,
                    @PublishedAt, @CreatedAt, @Status, @ContentType, @AuthorId, @CategoryId, @FeaturedImageId, @CommentsEnabled, @RequiresSignIn, @AuthorName)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Title", article.Title);
        command.Parameters.AddWithValue("@Slug", article.Slug);
        command.Parameters.AddWithValue("@PublishedVersionId", (object?)article.PublishedVersionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@PublishedAt", (object?)article.PublishedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", article.CreatedAt);
        command.Parameters.AddWithValue("@Status", (int)article.Status);
        command.Parameters.AddWithValue("@ContentType", (int)article.ContentType);
        command.Parameters.AddWithValue("@AuthorId", (object?)article.AuthorId ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", (object?)article.CategoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("@FeaturedImageId", (object?)article.FeaturedImageId ?? DBNull.Value);
        command.Parameters.AddWithValue("@CommentsEnabled", article.CommentsEnabled);
        command.Parameters.AddWithValue("@RequiresSignIn", article.RequiresSignIn);
        command.Parameters.AddWithValue("@AuthorName", (object?)article.AuthorName ?? DBNull.Value);

        var id = await command.ExecuteScalarAsync();
        article.Id = Convert.ToInt32(id);

        return article;
    }

    public async Task<Article> UpdateAsync(Article article)
    {
        const string sql = @"
            UPDATE articles
            SET title = @Title, slug = @Slug, published_version_id = @PublishedVersionId,
                published_at = @PublishedAt, updated_at = @UpdatedAt, status = @Status,
                category_id = @CategoryId, featured_image_id = @FeaturedImageId, comments_enabled = @CommentsEnabled,
                requires_sign_in = @RequiresSignIn, author_name = @AuthorName
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", article.Id);
        command.Parameters.AddWithValue("@Title", article.Title);
        command.Parameters.AddWithValue("@Slug", article.Slug);
        command.Parameters.AddWithValue("@PublishedVersionId", (object?)article.PublishedVersionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@PublishedAt", (object?)article.PublishedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", article.UpdatedAt ?? DateTime.UtcNow);
        command.Parameters.AddWithValue("@Status", (int)article.Status);
        command.Parameters.AddWithValue("@CategoryId", (object?)article.CategoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("@FeaturedImageId", (object?)article.FeaturedImageId ?? DBNull.Value);
        command.Parameters.AddWithValue("@CommentsEnabled", article.CommentsEnabled);
        command.Parameters.AddWithValue("@RequiresSignIn", article.RequiresSignIn);
        command.Parameters.AddWithValue("@AuthorName", (object?)article.AuthorName ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return article;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = await DataSource.OpenConnectionAsync();

        // Null out article_id on navigation items referencing this article
        await using (var clearCommand = new NpgsqlCommand(
            "UPDATE navigation_items SET article_id = NULL WHERE article_id = @Id", connection))
        {
            clearCommand.Parameters.AddWithValue("@Id", id);
            await clearCommand.ExecuteNonQueryAsync();
        }

        await using var deleteCommand = new NpgsqlCommand("DELETE FROM articles WHERE id = @Id", connection);
        deleteCommand.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM articles WHERE slug = @Slug AND id != @ExcludeId"
            : "SELECT COUNT(*) FROM articles WHERE slug = @Slug";

        var parameters = excludeId.HasValue
            ? new[] { new NpgsqlParameter("@Slug", slug), new NpgsqlParameter("@ExcludeId", excludeId.Value) }
            : new[] { new NpgsqlParameter("@Slug", slug) };

        var count = await ExecuteScalarIntAsync(sql, parameters);
        return count > 0;
    }

    public async Task UpdateTagsAsync(int articleId, IEnumerable<int> tagIds)
    {
        await using var connection = await DataSource.OpenConnectionAsync();

        // Delete existing tags
        await using (var deleteCommand = new NpgsqlCommand("DELETE FROM article_tags WHERE article_id = @ArticleId", connection))
        {
            deleteCommand.Parameters.AddWithValue("@ArticleId", articleId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        // Insert new tags
        var tagIdList = tagIds.ToList();
        if (tagIdList.Count > 0)
        {
            var insertSql = "INSERT INTO article_tags (article_id, tag_id) VALUES " +
                string.Join(", ", tagIdList.Select((_, i) => $"(@ArticleId, @TagId{i})"));

            await using var insertCommand = new NpgsqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@ArticleId", articleId);
            for (int i = 0; i < tagIdList.Count; i++)
            {
                insertCommand.Parameters.AddWithValue($"@TagId{i}", tagIdList[i]);
            }
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    public async Task SetPublishedVersionAsync(int articleId, int? versionId)
    {
        const string sql = "UPDATE articles SET published_version_id = @VersionId WHERE id = @ArticleId";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ArticleId", articleId);
        command.Parameters.AddWithValue("@VersionId", (object?)versionId ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static Article MapArticle(NpgsqlDataReader reader)
    {
        var authorId = GetNullable<int>(reader, "author_id");

        var article = new Article
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Slug = reader.GetString(reader.GetOrdinal("slug")),
            ContentType = (ContentType)reader.GetInt32(reader.GetOrdinal("content_type")),
            Content = GetNullableString(reader, "content"),
            Layout = GetNullableString(reader, "layout"),
            Excerpt = GetNullableString(reader, "excerpt"),
            MetaTitle = GetNullableString(reader, "meta_title"),
            MetaDescription = GetNullableString(reader, "meta_description"),
            PageStyle = GetNullableString(reader, "page_style"),
            FeaturedImageSize = GetNullableString(reader, "featured_image_size"),
            PublishedAt = GetNullable<DateTime>(reader, "published_at"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at"),
            Status = (ArticleStatus)reader.GetInt32(reader.GetOrdinal("status")),
            CommentsEnabled = reader.GetBoolean(reader.GetOrdinal("comments_enabled")),
            RequiresSignIn = reader.GetBoolean(reader.GetOrdinal("requires_sign_in")),
            AuthorId = authorId,
            AuthorName = GetNullableString(reader, "article_author_name"),
            CategoryId = GetNullable<int>(reader, "category_id"),
            SeriesId = GetNullable<int>(reader, "series_id"),
            SeriesOrder = GetNullable<int>(reader, "series_order"),
            PublishedVersionId = GetNullable<int>(reader, "published_version_id"),
            CurrentVersionNumber = GetNullable<int>(reader, "current_version_number"),
            TotalVersions = reader.GetInt32(reader.GetOrdinal("total_versions"))
        };

        // Map author if present
        if (authorId.HasValue)
        {
            article.Author = new Author
            {
                Id = reader.GetInt32(reader.GetOrdinal("author_id_nav")),
                Email = reader.GetString(reader.GetOrdinal("author_email")),
                Name = reader.GetString(reader.GetOrdinal("linked_author_name")),
                Bio = GetNullableString(reader, "author_bio"),
                AvatarUrl = GetNullableString(reader, "author_avatar")
            };
        }

        // Map featured image if present
        var featuredImageId = GetNullable<int>(reader, "featured_image_id");
        if (featuredImageId.HasValue)
        {
            article.FeaturedImageId = featuredImageId.Value;
            article.FeaturedImage = new MediaItem
            {
                Id = featuredImageId.Value,
                Path = reader.GetString(reader.GetOrdinal("featured_image_path")),
                ThumbnailPath = GetNullableString(reader, "featured_image_thumbnail_path"),
                MediumPath = GetNullableString(reader, "featured_image_medium_path")
            };
        }

        // Map category if present
        var catId = GetNullable<int>(reader, "cat_id");
        if (catId.HasValue)
        {
            article.Category = new Category
            {
                Id = catId.Value,
                Name = reader.GetString(reader.GetOrdinal("cat_name")),
                Slug = reader.GetString(reader.GetOrdinal("cat_slug")),
                Description = GetNullableString(reader, "cat_desc")
            };
        }

        // Map series if present
        var seriesIdNav = GetNullable<int>(reader, "series_id_nav");
        if (seriesIdNav.HasValue)
        {
            article.Series = new ArticleSeries
            {
                Id = seriesIdNav.Value,
                Name = reader.GetString(reader.GetOrdinal("series_name")),
                Slug = reader.GetString(reader.GetOrdinal("series_slug"))
            };
        }

        // Map tags
        var tagsJson = reader.GetString(reader.GetOrdinal("tags"));
        var tagDataList = JsonSerializer.Deserialize<List<TagData>>(tagsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TagData>();
        article.ArticleTags = tagDataList.Select(t => new ArticleTag
        {
            ArticleId = article.Id,
            TagId = t.Id,
            Tag = new Tag { Id = t.Id, Name = t.Name, Slug = t.Slug }
        }).ToList();

        return article;
    }

    private class TagData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
