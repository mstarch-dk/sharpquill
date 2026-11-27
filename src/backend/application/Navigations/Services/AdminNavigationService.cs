using SharpQuill.Application.Common;
using SharpQuill.Application.Navigations.DTOs;
using SharpQuill.Core.Navigations;


namespace SharpQuill.Application.Navigations.Services;

public class AdminNavigationService : IAdminNavigationService
{
    private readonly IAdminNavigationRepository _navigationRepository;

    public AdminNavigationService(IAdminNavigationRepository navigationRepository)
    {
        _navigationRepository = navigationRepository;
    }

    public async Task<IReadOnlyList<NavigationSummaryDto>> GetAllAsync()
    {
        var navigations = await _navigationRepository.GetAllAsync();
        return navigations.Select(n => new NavigationSummaryDto(n.Id, n.Name, n.Slug)).ToList();
    }

    public async Task<NavigationAdminDto?> GetByIdAsync(int id)
    {
        var navigation = await _navigationRepository.GetByIdWithItemsAsync(id);
        if (navigation == null) return null;

        var items = navigation.Items.Select(item => new NavigationItemAdminDto(
            item.Id,
            item.Label,
            item.ItemType,
            item.ArticleId,
            item.Article?.Title,
            item.Article?.Slug,
            item.SystemRoute,
            item.Url,
            item.ParentId,
            item.Position,
            item.OpenInNewTab
        )).ToList();

        return new NavigationAdminDto(navigation.Id, navigation.Name, navigation.Slug, items);
    }

    public async Task<(bool Success, string? Error)> SaveItemsAsync(int navigationId, NavigationItemsBulkSaveDto dto)
    {
        var navigation = await _navigationRepository.GetByIdWithItemsAsync(navigationId);
        if (navigation == null)
        {
            return (false, "Navigation not found");
        }

        var unsafeItem = dto.Items.FirstOrDefault(i => !UrlSafety.IsSafe(i.Url));
        if (unsafeItem != null)
        {
            return (false, $"The URL of '{unsafeItem.Label}' is not allowed. Use http, https, mailto, tel or a relative path.");
        }

        var items = dto.Items.Select(i => new NavigationItem
        {
            NavigationId = navigationId,
            ParentId = i.ParentId,
            Label = i.Label,
            ItemType = i.ItemType,
            ArticleId = i.ArticleId,
            SystemRoute = i.SystemRoute,
            Url = i.Url,
            Position = i.Position,
            OpenInNewTab = i.OpenInNewTab
        }).ToList();

        await _navigationRepository.ReplaceItemsAsync(navigationId, items);
        return (true, null);
    }
}
