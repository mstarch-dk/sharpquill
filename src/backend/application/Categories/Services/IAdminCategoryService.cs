using SharpQuill.Application.Categories.DTOs;

namespace SharpQuill.Application.Categories.Services;

public interface IAdminCategoryService
{
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<(CategoryDto? Category, string? Error)> CreateAsync(CategoryCreateDto dto);
    Task<(CategoryDto? Category, string? Error)> UpdateAsync(int id, CategoryUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
