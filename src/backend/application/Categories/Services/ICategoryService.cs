using SharpQuill.Application.Categories.DTOs;

namespace SharpQuill.Application.Categories.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetBySlugAsync(string slug);
}
