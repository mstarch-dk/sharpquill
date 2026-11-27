using SharpQuill.Application.Authors.DTOs;

namespace SharpQuill.Application.Authors.Services;

public interface IAdminAuthorService
{
    Task<IReadOnlyList<AuthorAdminDto>> GetAllAsync();
    Task<AuthorAdminDto?> GetByIdAsync(int id);
    Task<(AuthorAdminDto? Author, string? Error)> CreateAsync(AuthorCreateDto dto);
    Task<(AuthorAdminDto? Author, string? Error)> UpdateAsync(int id, AuthorUpdateDto dto, int requestingAuthorId);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int requestingAuthorId);
}
