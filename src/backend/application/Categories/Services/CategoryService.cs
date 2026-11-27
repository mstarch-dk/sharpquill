using SharpQuill.Application.Categories.DTOs;
using SharpQuill.Core.Categories;
using SharpQuill.Application.Categories.Repositories;

namespace SharpQuill.Application.Categories.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllWithArticleCountAsync();
        return categories.Select(c =>
        {
            var articleCount = (c as CategoryWithCount)?.ArticleCount ?? 0;
            return new CategoryDto(c.Id, c.Name, c.Slug, c.Description, articleCount);
        }).ToList();
    }

    public async Task<CategoryDto?> GetBySlugAsync(string slug)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug);
        return category == null
            ? null
            : new CategoryDto(category.Id, category.Name, category.Slug, category.Description, 0);
    }
}
