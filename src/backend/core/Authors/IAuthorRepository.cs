
namespace SharpQuill.Core.Authors;

public interface IAuthorRepository
{
    Task<Author?> GetByIdAsync(int id);
    Task<Author?> GetByEmailAsync(string email);
    Task<Author?> GetByCertificateThumbprintAsync(string thumbprint);
    Task<bool> UpdateCertificateThumbprintAsync(int authorId, string? thumbprint);
}
