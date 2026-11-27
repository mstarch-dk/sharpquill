using SharpQuill.Application.Navigations.DTOs;

namespace SharpQuill.Application.Navigations.Services;

public interface IAdminNavigationService
{
    Task<IReadOnlyList<NavigationSummaryDto>> GetAllAsync();
    Task<NavigationAdminDto?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> SaveItemsAsync(int navigationId, NavigationItemsBulkSaveDto dto);
}
