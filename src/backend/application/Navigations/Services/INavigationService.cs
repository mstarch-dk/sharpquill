using SharpQuill.Application.Navigations.DTOs;

namespace SharpQuill.Application.Navigations.Services;

public interface INavigationService
{
    Task<NavigationDto?> GetBySlugAsync(string slug);
}
