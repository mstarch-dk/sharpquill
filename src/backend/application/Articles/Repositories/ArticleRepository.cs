using SharpQuill.Core.Common;
using SharpQuill.Core.Articles;
using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;
using SharpQuill.Core.Tags;

namespace SharpQuill.Application.Articles.Repositories;

public class ArticleRepository : BaseRepository, IArticleRepository
{
    private const int ArticleType = (int)ContentType.Article;

    // Public reads only: public_articles is the view of Published articles/pages whose publish date has passed
    private const string BaseSelectSql = @"
        SELECT
            a.id, a.title, a.slug, a.content_type, a.comments_enabled, a.requires_sign_in,
            ac.content, ac.layout, ac.excerpt, ac.meta_title, ac.meta_description, ac.page_style, ac.featured_image_size,
            a.published_at, a.created_at, a.updated_at, a.status,
            a.author_id, au.name AS linked_author_name, au.avatar_url AS author_avatar_url,
            a.author_name AS article_author_name,
            a.category_id, c.name AS category_name, c.slug AS category_slug,
            a.series_id, a.series_order, s.name AS series_name, s.slug AS series_slug,
            a.featured_image_id, mi.path AS featured_image_path,
            mi.thumbnail_path AS featured_image_thumbnail_path, mi.medium_path AS featured_image_medium_path,
            COALESCE(
                (SELECT json_agg(json_build_object('id', t.id, 'name', t.name, 'slug', t.slug))
                 FROM article_tags at
                 JOIN tags t ON at.tag_id = t.id
                 WHERE at.article_id = a.id), '[]'
            ) AS tags_json
        FROM public_articles a
        LEFT JOIN article_contents ac ON a.published_version_id = ac.id
        LEFT JOIN authors au ON a.author_id = au.id
        LEFT JOIN categories c ON a.category_id = c.id
        LEFT JOIN series s ON a.series_id = s.id
        LEFT JOIN media mi ON a.featured_image_id = mi.id";

    public ArticleRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<Article?> GetBySlugAsync(string slug, ContentType contentType = ContentType.Article)
    {
        var sql = $@"{BaseSelectSql}
            WHERE a.slug = @slug AND a.content_type = @contentType";

        return await QuerySingleOrDefaultAsync(sql, MapArticle,
            new NpgsqlParameter("@slug", slug),
            new NpgsqlParameter("@contentType", (object)(int)contentType));
    }

    public Task<(IEnumerable<Article> Articles, int TotalCount)> GetPublishedAsync(PaginationParams pagination) =>
        GetPagedArticlesAsync("TRUE", pagination);

    public Task<(IEnumerable<Article> Articles, int TotalCount)> GetByCategorySlugAsync(string categorySlug, PaginationParams pagination) =>
        GetPagedArticlesAsync(
            "a.category_id = (SELECT id FROM categories WHERE slug = @slug)",
            pagination, ("@slug", categorySlug));

    public Task<(IEnumerable<Article> Articles, int TotalCount)> GetByTagSlugAsync(string tagSlug, PaginationParams pagination) =>
        GetPagedArticlesAsync(
            @"EXISTS (SELECT 1 FROM article_tags at JOIN tags t ON at.tag_id = t.id
                      WHERE at.article_id = a.id AND t.slug = @slug)",
            pagination, ("@slug", tagSlug));

    public async Task<IEnumerable<Article>> GetFeaturedAsync(int count = 5)
    {
        var sql = $@"{BaseSelectSql}
            WHERE a.content_type = {ArticleType}
            ORDER BY a.published_at DESC
            LIMIT @limit";

        return await QueryAsync(sql, MapArticle, new NpgsqlParameter("@limit", count));
    }

    /// <summary>
    /// One page of public blog articles (newest first) matching <paramref name="filter"/>, a predicate on alias
    /// <c>a</c>, plus the total count. The count and the page share the filter and its parameters.
    /// </summary>
    private async Task<(IEnumerable<Article> Articles, int TotalCount)> GetPagedArticlesAsync(
        string filter, PaginationParams pagination, params (string Name, object Value)[] filterParameters)
    {
        var where = $"WHERE a.content_type = {ArticleType} AND {filter}";
        // An NpgsqlParameter belongs to one command, so each query gets its own instances
        NpgsqlParameter[] Parameters() => filterParameters.Select(p => new NpgsqlParameter(p.Name, p.Value)).ToArray();

        var totalCount = await ExecuteScalarIntAsync($"SELECT COUNT(*) FROM public_articles a {where}", Parameters());

        var sql = $@"{BaseSelectSql}
            {where}
            ORDER BY a.published_at DESC
            LIMIT @limit OFFSET @offset";

        var articles = await QueryAsync(sql, MapArticle, [
            .. Parameters(),
            new NpgsqlParameter("@limit", pagination.GetValidatedPageSize()),
            new NpgsqlParameter("@offset", pagination.GetOffset())]);

        return (articles, totalCount);
    }

    // Search config and parsed query, shared by the count and the page query. websearch_to_tsquery accepts any input
    // (quotes, "or", "-word", stray symbols) without syntax errors; a query of only stop words matches nothing.
    private const string SearchQueryCte = @"
        WITH q AS (
            SELECT c.cfg, websearch_to_tsquery(c.cfg, @query) AS tsq
            FROM (SELECT cms_search_config() AS cfg) AS c
        )";

    private const string SearchFilterSql = @"
        a.content_type = @contentType
        AND a.search_vector @@ q.tsq
        AND (@includeMembersOnly OR NOT a.requires_sign_in)";

    public async Task<(IReadOnlyList<ArticleSearchHit> Hits, int TotalCount)> SearchPublishedAsync(
        string query, bool includeMembersOnly, PaginationParams pagination)
    {
        NpgsqlParameter[] FilterParameters() =>
        [
            new NpgsqlParameter("@query", query),
            new NpgsqlParameter("@contentType", (object)(int)ContentType.Article),
            new NpgsqlParameter("@includeMembersOnly", includeMembersOnly)
        ];

        var countSql = $@"{SearchQueryCte}
            SELECT COUNT(*) FROM public_articles a CROSS JOIN q
            WHERE {SearchFilterSql}";
        var totalCount = await ExecuteScalarIntAsync(countSql, FilterParameters());
        if (totalCount == 0)
        {
            return ([], 0);
        }

        // Rank and page first; the snippet (ts_headline reads the whole body) is built only for the rows of the page.
        var sql = $@"{SearchQueryCte},
        hits AS (
            SELECT a.id, ts_rank(a.search_vector, q.tsq) AS rank, a.published_at
            FROM public_articles a CROSS JOIN q
            WHERE {SearchFilterSql}
            ORDER BY rank DESC, a.published_at DESC NULLS LAST, a.id DESC
            LIMIT @limit OFFSET @offset
        )
        SELECT b.*,
            ts_headline(q.cfg, cms_article_body_text(b.content, b.layout), q.tsq,
                'StartSel=' || chr(2) || ', StopSel=' || chr(3)
                || ', MaxFragments=2, MaxWords=30, MinWords=12, FragmentDelimiter="" … ""') AS search_snippet
        FROM ({BaseSelectSql}
            WHERE a.id IN (SELECT id FROM hits)) AS b
        JOIN hits h ON h.id = b.id
        CROSS JOIN q
        ORDER BY h.rank DESC, h.published_at DESC NULLS LAST, h.id DESC";

        var hits = await QueryAsync(sql,
            reader => new ArticleSearchHit(MapArticle(reader), GetNullableString(reader, "search_snippet") ?? string.Empty),
            [
                .. FilterParameters(),
                new NpgsqlParameter("@limit", pagination.GetValidatedPageSize()),
                new NpgsqlParameter("@offset", pagination.GetOffset())
            ]);

        return (hits, totalCount);
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
            AuthorName = GetNullableString(reader, "article_author_name")
        };

        if (authorId.HasValue)
        {
            article.Author = new Author
            {
                Id = authorId.Value,
                Name = reader.GetString(reader.GetOrdinal("linked_author_name")),
                AvatarUrl = GetNullableString(reader, "author_avatar_url")
            };
        }

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

        var categoryId = GetNullable<int>(reader, "category_id");
        if (categoryId.HasValue)
        {
            article.CategoryId = categoryId.Value;
            article.Category = new Category
            {
                Id = categoryId.Value,
                Name = reader.GetString(reader.GetOrdinal("category_name")),
                Slug = reader.GetString(reader.GetOrdinal("category_slug"))
            };
        }

        var seriesId = GetNullable<int>(reader, "series_id");
        if (seriesId.HasValue)
        {
            article.SeriesId = seriesId.Value;
            article.SeriesOrder = GetNullable<int>(reader, "series_order");
            article.Series = new ArticleSeries
            {
                Id = seriesId.Value,
                Name = reader.GetString(reader.GetOrdinal("series_name")),
                Slug = reader.GetString(reader.GetOrdinal("series_slug"))
            };
        }

        var tagsJson = reader.GetString(reader.GetOrdinal("tags_json"));
        article.ArticleTags = ParseTagsJson(tagsJson, article.Id);

        return article;
    }

    private static List<ArticleTag> ParseTagsJson(string json, int articleId)
    {
        var articleTags = new List<ArticleTag>();

        if (string.IsNullOrEmpty(json) || json == "[]")
            return articleTags;

        // json_build_object emits lower-case keys (id, name, slug); match them to the PascalCase properties
        var tagsData = System.Text.Json.JsonSerializer.Deserialize<List<TagData>>(json, TagJsonOptions);
        if (tagsData == null)
            return articleTags;

        foreach (var tagData in tagsData)
        {
            articleTags.Add(new ArticleTag
            {
                ArticleId = articleId,
                TagId = tagData.Id,
                Tag = new Tag
                {
                    Id = tagData.Id,
                    Name = tagData.Name,
                    Slug = tagData.Slug
                }
            });
        }

        return articleTags;
    }

    private static readonly System.Text.Json.JsonSerializerOptions TagJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private class TagData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
