using SharpQuill.Application.Authors.DTOs;
using SharpQuill.Core.Auth;
using SharpQuill.Core.Authors;


namespace SharpQuill.Application.Authors.Services;

public class AdminAuthorService : IAdminAuthorService
{
    private readonly IAdminAuthorRepository _authorRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AdminAuthorService(IAdminAuthorRepository authorRepository, IRefreshTokenRepository refreshTokenRepository)
    {
        _authorRepository = authorRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<IReadOnlyList<AuthorAdminDto>> GetAllAsync()
    {
        var authors = await _authorRepository.GetAllAsync();
        return authors.Select(MapToDto).ToList();
    }

    public async Task<AuthorAdminDto?> GetByIdAsync(int id)
    {
        var author = await _authorRepository.GetByIdAsync(id);
        return author == null ? null : MapToDto(author);
    }

    public async Task<(AuthorAdminDto? Author, string? Error)> CreateAsync(AuthorCreateDto dto)
    {
        var inputError = ValidateInput(dto.Email, dto.Name, dto.Role);
        if (inputError != null)
        {
            return (null, inputError);
        }

        if (!AuthorRules.IsValidPassword(dto.Password))
        {
            return (null, AuthorRules.PasswordPolicyMessage);
        }

        var email = AuthorRules.NormalizeEmail(dto.Email);
        if (await _authorRepository.EmailExistsAsync(email))
        {
            return (null, DuplicateEmailMessage);
        }

        var author = new Author
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            Name = dto.Name.Trim(),
            Role = dto.Role,
            Bio = dto.Bio,
            AvatarUrl = dto.AvatarUrl,
            CertificateThumbprint = dto.CertificateThumbprint,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _authorRepository.CreateAsync(author);
        return (MapToDto(created), null);
    }

    public async Task<(AuthorAdminDto? Author, string? Error)> UpdateAsync(int id, AuthorUpdateDto dto, int requestingAuthorId)
    {
        var existing = await _authorRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (null, "Author not found");
        }

        if (id == requestingAuthorId && existing.Role != dto.Role)
        {
            return (null, "You cannot change your own role");
        }

        var inputError = ValidateInput(dto.Email, dto.Name, dto.Role);
        if (inputError != null)
        {
            return (null, inputError);
        }

        // null or "" keeps the current password; anything else is a new password and must meet the policy.
        var newPassword = !string.IsNullOrEmpty(dto.Password);
        if (newPassword && !AuthorRules.IsValidPassword(dto.Password))
        {
            return (null, AuthorRules.PasswordPolicyMessage);
        }

        var email = AuthorRules.NormalizeEmail(dto.Email);
        if (await _authorRepository.EmailExistsAsync(email, id))
        {
            return (null, DuplicateEmailMessage);
        }

        var sessionsInvalidated = existing.Role != dto.Role || newPassword;

        existing.Email = email;
        existing.Name = dto.Name.Trim();
        existing.Role = dto.Role;
        existing.Bio = dto.Bio;
        existing.AvatarUrl = dto.AvatarUrl;
        existing.CertificateThumbprint = dto.CertificateThumbprint;

        if (newPassword)
        {
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
        }

        await _authorRepository.UpdateAsync(existing);

        if (sessionsInvalidated)
        {
            await _refreshTokenRepository.RevokeAllForAuthorAsync(id);
        }

        return (MapToDto(existing), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int requestingAuthorId)
    {
        if (id == requestingAuthorId)
        {
            return (false, "You cannot delete your own account");
        }

        var existing = await _authorRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return (false, "Author not found");
        }

        var deleted = await _authorRepository.DeleteAsync(id);
        return (deleted, null);
    }

    private const string DuplicateEmailMessage = "An author with this email already exists";

    private static readonly string[] ValidRoles = ["admin", "user"];

    private static string? ValidateInput(string email, string name, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || !AuthorRules.IsValidEmail(email))
        {
            return "Email must be a valid email address";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required";
        }

        if (!ValidRoles.Contains(role))
        {
            return "Invalid role. Must be 'admin' or 'user'";
        }

        return null;
    }

    private static AuthorAdminDto MapToDto(Author author)
    {
        return new AuthorAdminDto(
            author.Id,
            author.Email,
            author.Name,
            author.Role,
            author.Bio,
            author.AvatarUrl,
            author.CertificateThumbprint,
            author.CreatedAt,
            author.UpdatedAt
        );
    }
}
