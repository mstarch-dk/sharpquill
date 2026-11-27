
namespace SharpQuill.Core.Navigations;

public interface IAdminNavigationRepository
{
    Task<IEnumerable<Navigation>> GetAllAsync();
    Task<Navigation?> GetByIdWithItemsAsync(int id);
    Task ReplaceItemsAsync(int navigationId, IEnumerable<NavigationItem> items);
}
