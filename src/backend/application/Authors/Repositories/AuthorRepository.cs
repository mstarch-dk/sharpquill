using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Authors;

namespace SharpQuill.Application.Authors.Repositories;

public class AuthorRepository : BaseRepository, IAuthorRepository
{
    public AuthorRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, role, created_at, updated_at
            FROM authors
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapAuthor, new NpgsqlParameter("@Id", id));
    }

    public async Task<Author?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, role, created_at, updated_at
            FROM authors
            WHERE lower(email) = lower(trim(@Email))";

        return await QuerySingleOrDefaultAsync(sql, MapAuthor, new NpgsqlParameter("@Email", email));
    }

    public async Task<Author?> GetByCertificateThumbprintAsync(string thumbprint)
    {
        const string sql = @"
            SELECT id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, role, created_at, updated_at
            FROM authors
            WHERE certificate_thumbprint = @Thumbprint";

        return await QuerySingleOrDefaultAsync(sql, MapAuthor, new NpgsqlParameter("@Thumbprint", thumbprint));
    }

    public async Task<bool> UpdateCertificateThumbprintAsync(int authorId, string? thumbprint)
    {
        const string sql = @"
            UPDATE authors
            SET certificate_thumbprint = @Thumbprint, updated_at = @UpdatedAt
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", authorId);
        command.Parameters.AddWithValue("@Thumbprint", (object?)thumbprint ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
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
