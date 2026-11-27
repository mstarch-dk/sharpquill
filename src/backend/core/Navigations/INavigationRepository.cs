
namespace SharpQuill.Core.Navigations;

public interface INavigationRepository
{
    Task<Navigation?> GetBySlugWithItemsAsync(string slug);
}
