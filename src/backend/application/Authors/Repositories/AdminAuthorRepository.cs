using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Authors;

namespace SharpQuill.Application.Authors.Repositories;

public class AdminAuthorRepository : BaseRepository, IAdminAuthorRepository
{
    public AdminAuthorRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, role, created_at, updated_at
            FROM authors
            ORDER BY name";

        return await QueryAsync(sql, MapAuthor);
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, role, created_at, updated_at
            FROM authors
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapAuthor, new NpgsqlParameter("@Id", id));
    }

    public async Task<Author> CreateAsync(Author author)
    {
        const string sql = @"
            INSERT INTO authors (email, password_hash, name, role, bio, avatar_url, certificate_thumbprint, created_at)
            VALUES (@Email, @PasswordHash, @Name, @Role, @Bio, @AvatarUrl, @CertificateThumbprint, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Email", author.Email);
        command.Parameters.AddWithValue("@PasswordHash", author.PasswordHash);
        command.Parameters.AddWithValue("@Name", author.Name);
        command.Parameters.AddWithValue("@Role", author.Role);
        command.Parameters.AddWithValue("@Bio", (object?)author.Bio ?? DBNull.Value);
        command.Parameters.AddWithValue("@AvatarUrl", (object?)author.AvatarUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@CertificateThumbprint", (object?)author.CertificateThumbprint ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", author.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        author.Id = Convert.ToInt32(id);

        return author;
    }

    public async Task<Author> UpdateAsync(Author author)
    {
        const string sql = @"
            UPDATE authors
            SET email = @Email, password_hash = @PasswordHash, name = @Name, role = @Role,
                bio = @Bio, avatar_url = @AvatarUrl, certificate_thumbprint = @CertificateThumbprint,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", author.Id);
        command.Parameters.AddWithValue("@Email", author.Email);
        command.Parameters.AddWithValue("@PasswordHash", author.PasswordHash);
        command.Parameters.AddWithValue("@Name", author.Name);
        command.Parameters.AddWithValue("@Role", author.Role);
        command.Parameters.AddWithValue("@Bio", (object?)author.Bio ?? DBNull.Value);
        command.Parameters.AddWithValue("@AvatarUrl", (object?)author.AvatarUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@CertificateThumbprint", (object?)author.CertificateThumbprint ?? DBNull.Value);
        var now = DateTime.UtcNow;
        command.Parameters.AddWithValue("@UpdatedAt", now);

        await command.ExecuteNonQueryAsync();
        author.UpdatedAt = now;

        return author;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM authors WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? "SELECT COUNT(*) FROM authors WHERE lower(email) = lower(@Email) AND id != @ExcludeId"
            : "SELECT COUNT(*) FROM authors WHERE lower(email) = lower(@Email)";

        var parameters = excludeId.HasValue
            ? new[] { new NpgsqlParameter("@Email", email), new NpgsqlParameter("@ExcludeId", excludeId.Value) }
            : new[] { new NpgsqlParameter("@Email", email) };

        var count = await ExecuteScalarIntAsync(sql, parameters);
        return count > 0;
    }

    private static Author MapAuthor(NpgsqlDataReader reader)
    {
        return new Author
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Bio = GetNullableString(reader, "bio"),
            AvatarUrl = GetNullableString(reader, "avatar_url"),
            CertificateThumbprint = GetNullableString(reader, "certificate_thumbprint"),
            Role = reader.GetString(reader.GetOrdinal("role")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at")
        };
    }
}
