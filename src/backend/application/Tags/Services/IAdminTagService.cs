using SharpQuill.Application.Tags.DTOs;

namespace SharpQuill.Application.Tags.Services;

public interface IAdminTagService
{
    Task<TagDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<TagDto>> GetAllAsync();
    Task<(TagDto? Tag, string? Error)> CreateAsync(TagCreateDto dto);
    Task<(TagDto? Tag, string? Error)> UpdateAsync(int id, TagUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
