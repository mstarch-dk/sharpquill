using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Common;
using SharpQuill.Core.Media;

namespace SharpQuill.Application.Media.Repositories;

public class MediaRepository : BaseRepository, IMediaRepository
{
    public MediaRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    private const string SelectColumns = @"
        m.id, m.filename, m.original_filename, m.mime_type, m.size, m.path,
        m.thumbnail_path, m.medium_path, m.width, m.height,
        m.alt_text, m.created_at, m.author_id,
        a.id as author_id_nav, a.name as author_name, a.avatar_url as author_avatar";

    public async Task<MediaItem?> GetByIdAsync(int id)
    {
        var sql = $@"
            SELECT {SelectColumns}
            FROM media m
            INNER JOIN authors a ON m.author_id = a.id
            WHERE m.id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapMedia, new NpgsqlParameter("@Id", id));
    }

    public async Task<IEnumerable<MediaItem>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return Enumerable.Empty<MediaItem>();

        var paramNames = string.Join(", ", idList.Select((_, i) => $"@Id{i}"));
        var sql = $@"
            SELECT {SelectColumns}
            FROM media m
            INNER JOIN authors a ON m.author_id = a.id
            WHERE m.id IN ({paramNames})";

        var parameters = idList.Select((id, i) => new NpgsqlParameter($"@Id{i}", id)).ToArray();
        return await QueryAsync(sql, MapMedia, parameters);
    }

    public async Task<(IEnumerable<MediaItem> Items, int TotalCount)> GetAllAsync(PaginationParams pagination)
    {
        var countSql = "SELECT COUNT(*) FROM media";
        var totalCount = await ExecuteScalarIntAsync(countSql);

        var sql = $@"
            SELECT {SelectColumns}
            FROM media m
            INNER JOIN authors a ON m.author_id = a.id
            ORDER BY m.created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var items = await QueryAsync(sql, MapMedia,
            new NpgsqlParameter("@Limit", pagination.GetValidatedPageSize()),
            new NpgsqlParameter("@Offset", pagination.GetOffset()));

        return (items, totalCount);
    }

    public async Task<MediaItem> CreateAsync(MediaItem media)
    {
        const string sql = @"
            INSERT INTO media (filename, original_filename, mime_type, size, path,
                thumbnail_path, medium_path, width, height, alt_text, created_at, author_id)
            VALUES (@Filename, @OriginalFilename, @MimeType, @Size, @Path,
                @ThumbnailPath, @MediumPath, @Width, @Height, @AltText, @CreatedAt, @AuthorId)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Filename", media.Filename);
        command.Parameters.AddWithValue("@OriginalFilename", media.OriginalFilename);
        command.Parameters.AddWithValue("@MimeType", media.MimeType);
        command.Parameters.AddWithValue("@Size", media.Size);
        command.Parameters.AddWithValue("@Path", media.Path);
        command.Parameters.AddWithValue("@ThumbnailPath", (object?)media.ThumbnailPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@MediumPath", (object?)media.MediumPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@Width", (object?)media.Width ?? DBNull.Value);
        command.Parameters.AddWithValue("@Height", (object?)media.Height ?? DBNull.Value);
        command.Parameters.AddWithValue("@AltText", (object?)media.AltText ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", media.CreatedAt);
        command.Parameters.AddWithValue("@AuthorId", media.AuthorId);

        var id = await command.ExecuteScalarAsync();
        media.Id = Convert.ToInt32(id);

        return media;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM media WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<IReadOnlyList<MediaUsage>> GetUsagesAsync(MediaItem media)
    {
        // Text references are matched on the stored path without its extension ("2026/11/<guid>"), which is part of
        // the URL of the original and of its _medium/_thumb variants. Only the latest and the published version of an
        // article count: older versions are history, restoring one with a deleted image shows a broken image.
        const string sql = @"
            WITH current_versions AS (
                SELECT a.id AS article_id, a.title, a.content_type, ac.content, ac.layout
                FROM articles a
                JOIN article_contents ac ON ac.article_id = a.id
                WHERE ac.id = a.published_version_id
                   OR ac.version_number = (SELECT MAX(x.version_number) FROM article_contents x WHERE x.article_id = a.id)
            )
            SELECT kind, article_id, title, content_type FROM (
                SELECT 'featured_image' AS kind, a.id AS article_id, a.title, a.content_type, 0 AS sort
                FROM articles a
                WHERE a.featured_image_id = @Id
                UNION
                SELECT 'content', cv.article_id, cv.title, cv.content_type, 1
                FROM current_versions cv
                WHERE strpos(cv.content, @Stem) > 0
                UNION
                SELECT 'layout', cv.article_id, cv.title, cv.content_type, 2
                FROM current_versions cv
                WHERE cv.layout IS NOT NULL
                  AND (jsonb_path_exists(cv.layout, 'lax $.** ? (@.mediaId == $id)', jsonb_build_object('id', @Id))
                       OR strpos(cv.layout::text, @Stem) > 0)
                UNION
                SELECT 'setting', NULL, s.key, 0, 3
                FROM site_settings s
                WHERE strpos(s.value, @Stem) > 0
                UNION
                SELECT 'author_avatar', NULL, au.name, 0, 4
                FROM authors au
                WHERE strpos(au.avatar_url, @Stem) > 0
            ) usages
            ORDER BY sort, title";

        var stem = StripExtension(media.Path);
        return await QueryAsync(sql, reader => new MediaUsage(
                reader.GetString(reader.GetOrdinal("kind")),
                GetNullable<int>(reader, "article_id"),
                reader.GetString(reader.GetOrdinal("title")),
                reader.GetInt32(reader.GetOrdinal("content_type")) == (int)ContentType.Page),
            new NpgsqlParameter("@Id", media.Id),
            new NpgsqlParameter("@Stem", stem));
    }

    private static string StripExtension(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot > path.LastIndexOf('/') ? path[..dot] : path;
    }

    public async Task<IEnumerable<MediaItem>> GetRasterMediaWithoutVariantsAsync()
    {
        const string sql = @"
            SELECT m.id, m.filename, m.original_filename, m.mime_type, m.size, m.path,
                m.thumbnail_path, m.medium_path, m.width, m.height,
                m.alt_text, m.created_at, m.author_id,
                0 as author_id_nav, '' as author_name, null as author_avatar
            FROM media m
            WHERE m.thumbnail_path IS NULL
              AND m.mime_type IN ('image/jpeg', 'image/png', 'image/gif', 'image/webp')";

        return await QueryAsync(sql, MapMediaNoAuthor);
    }

    public async Task UpdateVariantPathsAsync(int id, string? thumbnailPath, string? mediumPath, int? width, int? height)
    {
        const string sql = @"
            UPDATE media
            SET thumbnail_path = @ThumbnailPath, medium_path = @MediumPath,
                width = @Width, height = @Height
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@ThumbnailPath", (object?)thumbnailPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@MediumPath", (object?)mediumPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@Width", (object?)width ?? DBNull.Value);
        command.Parameters.AddWithValue("@Height", (object?)height ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static MediaItem MapMedia(NpgsqlDataReader reader)
    {
        return new MediaItem
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Filename = reader.GetString(reader.GetOrdinal("filename")),
            OriginalFilename = reader.GetString(reader.GetOrdinal("original_filename")),
            MimeType = reader.GetString(reader.GetOrdinal("mime_type")),
            Size = reader.GetInt64(reader.GetOrdinal("size")),
            Path = reader.GetString(reader.GetOrdinal("path")),
            ThumbnailPath = GetNullableString(reader, "thumbnail_path"),
            MediumPath = GetNullableString(reader, "medium_path"),
            Width = GetNullable<int>(reader, "width"),
            Height = GetNullable<int>(reader, "height"),
            AltText = GetNullableString(reader, "alt_text"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            AuthorId = reader.GetInt32(reader.GetOrdinal("author_id")),
            Author = new Author
            {
                Id = reader.GetInt32(reader.GetOrdinal("author_id_nav")),
                Name = reader.GetString(reader.GetOrdinal("author_name")),
                AvatarUrl = GetNullableString(reader, "author_avatar")
            }
        };
    }

    private static MediaItem MapMediaNoAuthor(NpgsqlDataReader reader)
    {
        return new MediaItem
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Filename = reader.GetString(reader.GetOrdinal("filename")),
            OriginalFilename = reader.GetString(reader.GetOrdinal("original_filename")),
            MimeType = reader.GetString(reader.GetOrdinal("mime_type")),
            Size = reader.GetInt64(reader.GetOrdinal("size")),
            Path = reader.GetString(reader.GetOrdinal("path")),
            ThumbnailPath = GetNullableString(reader, "thumbnail_path"),
            MediumPath = GetNullableString(reader, "medium_path"),
            Width = GetNullable<int>(reader, "width"),
            Height = GetNullable<int>(reader, "height"),
            AltText = GetNullableString(reader, "alt_text"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            AuthorId = reader.GetInt32(reader.GetOrdinal("author_id")),
            Author = new Author { Id = 0, Name = string.Empty }
        };
    }
}
