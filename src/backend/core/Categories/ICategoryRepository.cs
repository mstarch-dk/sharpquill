
namespace SharpQuill.Core.Categories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllWithArticleCountAsync();
    Task<Category?> GetBySlugAsync(string slug);
}
