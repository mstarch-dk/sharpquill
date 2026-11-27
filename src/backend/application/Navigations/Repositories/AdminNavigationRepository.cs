using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Navigations;

namespace SharpQuill.Application.Navigations.Repositories;

public class AdminNavigationRepository : BaseRepository, IAdminNavigationRepository
{
    public AdminNavigationRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<Navigation>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, name, slug
            FROM navigations
            ORDER BY name";

        return await QueryAsync(sql, MapNavigation);
    }

    public async Task<Navigation?> GetByIdWithItemsAsync(int id)
    {
        const string navSql = @"
            SELECT id, name, slug
            FROM navigations
            WHERE id = @Id";

        var navigation = await QuerySingleOrDefaultAsync(navSql, MapNavigation,
            new NpgsqlParameter("@Id", id));

        if (navigation == null) return null;

        const string itemsSql = @"
            SELECT ni.id, ni.navigation_id, ni.parent_id, ni.label, ni.item_type,
                   ni.article_id, ni.system_route, ni.url, ni.position, ni.open_in_new_tab,
                   a.title AS article_title, a.slug AS article_slug
            FROM navigation_items ni
            LEFT JOIN articles a ON ni.article_id = a.id AND a.content_type = @PageType
            WHERE ni.navigation_id = @NavigationId
            ORDER BY ni.position";

        navigation.Items = await QueryAsync(itemsSql, MapNavigationItemAdmin,
            new NpgsqlParameter("@NavigationId", navigation.Id),
            new NpgsqlParameter("@PageType", (object)(int)ContentType.Page));

        return navigation;
    }

    public async Task ReplaceItemsAsync(int navigationId, IEnumerable<NavigationItem> items)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // Delete existing items
        await using (var deleteCommand = new NpgsqlCommand(
            "DELETE FROM navigation_items WHERE navigation_id = @NavigationId", connection, transaction))
        {
            deleteCommand.Parameters.AddWithValue("@NavigationId", navigationId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        // Insert new items
        foreach (var item in items)
        {
            const string insertSql = @"
                INSERT INTO navigation_items (navigation_id, parent_id, label, item_type,
                                             article_id, system_route, url, position, open_in_new_tab)
                VALUES (@NavigationId, @ParentId, @Label, @ItemType,
                        @ArticleId, @SystemRoute, @Url, @Position, @OpenInNewTab)";

            await using var command = new NpgsqlCommand(insertSql, connection, transaction);

            command.Parameters.AddWithValue("@NavigationId", navigationId);
            command.Parameters.AddWithValue("@ParentId", (object?)item.ParentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Label", item.Label);
            command.Parameters.AddWithValue("@ItemType", (int)item.ItemType);
            command.Parameters.AddWithValue("@ArticleId", (object?)item.ArticleId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SystemRoute", (object?)item.SystemRoute ?? DBNull.Value);
            command.Parameters.AddWithValue("@Url", (object?)item.Url ?? DBNull.Value);
            command.Parameters.AddWithValue("@Position", item.Position);
            command.Parameters.AddWithValue("@OpenInNewTab", item.OpenInNewTab);

            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private static Navigation MapNavigation(NpgsqlDataReader reader)
    {
        return new Navigation
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Slug = reader.GetString(reader.GetOrdinal("slug"))
        };
    }

    private static NavigationItem MapNavigationItemAdmin(NpgsqlDataReader reader)
    {
        var item = new NavigationItem
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            NavigationId = reader.GetInt32(reader.GetOrdinal("navigation_id")),
            ParentId = GetNullable<int>(reader, "parent_id"),
            Label = reader.GetString(reader.GetOrdinal("label")),
            ItemType = (NavigationItemType)reader.GetInt32(reader.GetOrdinal("item_type")),
            ArticleId = GetNullable<int>(reader, "article_id"),
            SystemRoute = GetNullableString(reader, "system_route"),
            Url = GetNullableString(reader, "url"),
            Position = reader.GetInt32(reader.GetOrdinal("position")),
            OpenInNewTab = reader.GetBoolean(reader.GetOrdinal("open_in_new_tab"))
        };

        var articleTitle = GetNullableString(reader, "article_title");
        var articleSlug = GetNullableString(reader, "article_slug");
        if (articleSlug != null)
        {
            item.Article = new Article { Title = articleTitle ?? string.Empty, Slug = articleSlug };
        }

        return item;
    }
}
