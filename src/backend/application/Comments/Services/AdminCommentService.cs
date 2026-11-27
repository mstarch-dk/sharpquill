using SharpQuill.Application.Comments.DTOs;
using SharpQuill.Application.Common.DTOs;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Common;

namespace SharpQuill.Application.Comments.Services;

public class AdminCommentService : IAdminCommentService
{
    private readonly IAdminCommentRepository _commentRepository;

    public AdminCommentService(IAdminCommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<PagedResultDto<CommentAdminDto>> GetAllAsync(PaginationParams pagination, CommentStatus? status = null)
    {
        var (comments, totalCount) = await _commentRepository.GetAllAsync(pagination, status);

        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = comments.Select(MapToAdminDto).ToList();
        var meta = new PaginationMeta(page, pageSize, totalCount, totalPages);

        return new PagedResultDto<CommentAdminDto>(data, meta);
    }

    public async Task<CommentAdminDto?> GetByIdAsync(int id)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        return comment == null ? null : MapToAdminDto(comment);
    }

    public async Task<(bool Success, string? Error)> ModerateAsync(int id, CommentStatus status)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null)
        {
            return (false, "Comment not found");
        }

        await _commentRepository.UpdateStatusAsync(id, status);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null)
        {
            return (false, "Comment not found");
        }

        await _commentRepository.DeleteAsync(id);
        return (true, null);
    }

    public async Task<CommentCountsDto> GetCountsAsync()
    {
        var (pending, approved, rejected) = await _commentRepository.GetCountsByStatusAsync();
        return new CommentCountsDto(pending, approved, rejected);
    }

    private static CommentAdminDto MapToAdminDto(Comment comment)
    {
        return new CommentAdminDto(
            comment.Id,
            comment.ArticleId,
            comment.ArticleTitle,
            comment.AuthorName,
            comment.AuthorEmail,
            comment.Content,
            comment.Status,
            comment.IpAddress,
            comment.ParentCommentId,
            comment.CreatedAt,
            comment.UpdatedAt
        );
    }
}
