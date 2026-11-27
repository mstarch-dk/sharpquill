
namespace SharpQuill.Core.Articles;

public interface IArticleContentRepository
{
    Task<ArticleContent?> GetByIdAsync(int id);
    Task<ArticleContent?> GetLatestVersionAsync(int articleId);
    Task<IEnumerable<ArticleContent>> GetVersionsByArticleIdAsync(int articleId);
    Task<ArticleContent> CreateVersionAsync(ArticleContent version);
    Task<int> GetNextVersionNumberAsync(int articleId);
}
