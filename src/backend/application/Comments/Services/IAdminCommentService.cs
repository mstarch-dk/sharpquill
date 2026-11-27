using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Common;

namespace SharpQuill.Application.Comments.Services;

public interface IAdminCommentService
{
    Task<PagedResultDto<CommentAdminDto>> GetAllAsync(PaginationParams pagination, CommentStatus? status = null);
    Task<CommentAdminDto?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> ModerateAsync(int id, CommentStatus status);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<CommentCountsDto> GetCountsAsync();
}
