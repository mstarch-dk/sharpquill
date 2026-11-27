using SharpQuill.Core.Common;

namespace SharpQuill.Core.Comments;

public interface IAdminCommentRepository
{
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetAllAsync(PaginationParams pagination, CommentStatus? status = null);
    Task<Comment?> GetByIdAsync(int id);
    Task UpdateStatusAsync(int id, CommentStatus status);
    Task<bool> DeleteAsync(int id);
    Task<(int Pending, int Approved, int Rejected)> GetCountsByStatusAsync();
}
