
namespace SharpQuill.Core.Tags;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllWithArticleCountAsync();
    Task<Tag?> GetBySlugAsync(string slug);
}
