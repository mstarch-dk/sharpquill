using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Core.Categories;


namespace SharpQuill.Application.Categories.Services;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly IAdminCategoryRepository _categoryRepository;

    public AdminCategoryService(IAdminCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;

        var articleCount = await _categoryRepository.GetArticleCountAsync(id);
        return MapToCategoryDto(category, articleCount);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var result = new List<CategoryDto>();

        foreach (var category in categories)
        {
            var articleCount = await _categoryRepository.GetArticleCountAsync(category.Id);
            result.Add(MapToCategoryDto(category, articleCount));
        }

        return result;
    }

    public async Task<(CategoryDto? Category, string? Error)> CreateAsync(CategoryCreateDto dto)
    {
        if (await _categoryRepository.SlugExistsAsync(dto.Slug))
        {
            return (null, "A category with this slug already exists");
        }

        var category = new Category
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.CreateAsync(category);
        return (MapToCategoryDto(created, 0), null);
    }

    public async Task<(CategoryDto? Category, string? Error)> UpdateAsync(int id, CategoryUpdateDto dto)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (null, "Category not found");
        }

        if (await _categoryRepository.SlugExistsAsync(dto.Slug, id))
        {
            return (null, "A category with this slug already exists");
        }

        existing.Name = dto.Name;
        existing.Slug = dto.Slug;
        existing.Description = dto.Description;

        await _categoryRepository.UpdateAsync(existing);

        var articleCount = await _categoryRepository.GetArticleCountAsync(id);
        return (MapToCategoryDto(existing, articleCount), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (false, "Category not found");
        }

        var articleCount = await _categoryRepository.GetArticleCountAsync(id);
        if (articleCount > 0)
        {
            return (false, $"Cannot delete category with {articleCount} associated article(s)");
        }

        var deleted = await _categoryRepository.DeleteAsync(id);
        return (deleted, null);
    }

    private static CategoryDto MapToCategoryDto(Category category, int articleCount)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            articleCount
        );
    }
}
