using SharpQuill.Application.Tags.DTOs;

namespace SharpQuill.Application.Tags.Services;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync();
    Task<TagDto?> GetBySlugAsync(string slug);
}
