using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Navigations;

namespace SharpQuill.Application.Navigations.Repositories;

public class NavigationRepository : BaseRepository, INavigationRepository
{
    public NavigationRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<Navigation?> GetBySlugWithItemsAsync(string slug)
    {
        // First get the navigation
        const string navSql = @"
            SELECT id, name, slug
            FROM navigations
            WHERE slug = @Slug";

        var navigation = await QuerySingleOrDefaultAsync(navSql, MapNavigation,
            new NpgsqlParameter("@Slug", slug));

        if (navigation == null) return null;

        // Then get items with article slug for URL resolution
        const string itemsSql = @"
            SELECT ni.id, ni.navigation_id, ni.parent_id, ni.label, ni.item_type,
                   ni.article_id, ni.system_route, ni.url, ni.position, ni.open_in_new_tab,
                   a.slug AS article_slug
            FROM navigation_items ni
            LEFT JOIN public_articles a ON ni.article_id = a.id AND a.content_type = @PageType
            WHERE ni.navigation_id = @NavigationId
            ORDER BY ni.position";

        navigation.Items = await QueryAsync(itemsSql, MapNavigationItem,
            new NpgsqlParameter("@NavigationId", navigation.Id),
            new NpgsqlParameter("@PageType", (object)(int)ContentType.Page));

        return navigation;
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

    private static NavigationItem MapNavigationItem(NpgsqlDataReader reader)
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

        // Attach article with slug for URL resolution
        var articleSlug = GetNullableString(reader, "article_slug");
        if (articleSlug != null)
        {
            item.Article = new Article { Slug = articleSlug };
        }

        return item;
    }
}
