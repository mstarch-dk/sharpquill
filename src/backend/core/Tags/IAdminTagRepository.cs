
namespace SharpQuill.Core.Tags;

public interface IAdminTagRepository
{
    Task<Tag?> GetByIdAsync(int id);
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag> CreateAsync(Tag tag);
    Task<Tag> UpdateAsync(Tag tag);
    Task<bool> DeleteAsync(int id);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    Task<int> GetArticleCountAsync(int tagId);
}
