using SharpQuill.Application.Common;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Core.Navigations;


namespace SharpQuill.Application.Navigations.Services;

public class NavigationService : INavigationService
{
    private readonly INavigationRepository _navigationRepository;

    public NavigationService(INavigationRepository navigationRepository)
    {
        _navigationRepository = navigationRepository;
    }

    public async Task<NavigationDto?> GetBySlugAsync(string slug)
    {
        var navigation = await _navigationRepository.GetBySlugWithItemsAsync(slug);
        if (navigation == null) return null;

        var items = navigation.Items.Select(item => new NavigationItemDto(
            item.Id,
            item.Label,
            ResolveUrl(item),
            item.ParentId,
            item.Position,
            item.OpenInNewTab
        )).ToList();

        return new NavigationDto(navigation.Id, navigation.Name, navigation.Slug, items);
    }

    private static string ResolveUrl(NavigationItem item)
    {
        return item.ItemType switch
        {
            NavigationItemType.Page => item.Article != null ? $"/{item.Article.Slug}" : "#",
            NavigationItemType.SystemRoute => item.SystemRoute ?? "#",
            NavigationItemType.ExternalUrl => UrlSafety.IsSafe(item.Url) ? item.Url ?? "#" : "#",
            _ => "#"
        };
    }
}
