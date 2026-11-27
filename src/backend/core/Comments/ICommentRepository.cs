namespace SharpQuill.Core.Comments;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetApprovedByArticleIdAsync(int articleId);
    Task<Comment> CreateAsync(Comment comment);
    Task<bool> ArticleExistsAsync(int articleId);
    Task<bool> CommentsEnabledAsync(int articleId);
    Task<bool> RequiresSignInAsync(int articleId);
    Task<Comment?> GetByIdAsync(int id);
}
