using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Auth;
using SharpQuill.Core.Authors;

namespace SharpQuill.Application.Auth.Repositories;

public class RefreshTokenRepository : BaseRepository, IRefreshTokenRepository
{
    public RefreshTokenRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        const string sql = @"
            SELECT rt.id, rt.token, rt.author_id, rt.expires_at, rt.created_at, rt.revoked_at, rt.replaced_by_token,
                   a.id as author_id_nav, a.email, a.password_hash, a.name, a.bio, a.avatar_url,
                   a.certificate_thumbprint, a.role, a.created_at as author_created_at, a.updated_at as author_updated_at
            FROM refresh_tokens rt
            INNER JOIN authors a ON rt.author_id = a.id
            WHERE rt.token = @Token";

        return await QuerySingleOrDefaultAsync(sql, MapRefreshToken, new NpgsqlParameter("@Token", TokenHasher.Hash(token)));
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        const string sql = @"
            INSERT INTO refresh_tokens (token, author_id, expires_at, created_at)
            VALUES (@Token, @AuthorId, @ExpiresAt, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Token", TokenHasher.Hash(refreshToken.Token));
        command.Parameters.AddWithValue("@AuthorId", refreshToken.AuthorId);
        command.Parameters.AddWithValue("@ExpiresAt", refreshToken.ExpiresAt);
        command.Parameters.AddWithValue("@CreatedAt", refreshToken.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        refreshToken.Id = Convert.ToInt32(id);

        return refreshToken;
    }

    public async Task<bool> RevokeAsync(int id, string? replacedByToken = null)
    {
        const string sql = @"
            UPDATE refresh_tokens
            SET revoked_at = @RevokedAt, replaced_by_token = @ReplacedByToken
            WHERE id = @Id AND revoked_at IS NULL";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@ReplacedByToken", replacedByToken == null ? DBNull.Value : TokenHasher.Hash(replacedByToken));

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task RevokeAllForAuthorAsync(int authorId)
    {
        const string sql = @"
            UPDATE refresh_tokens
            SET revoked_at = @RevokedAt
            WHERE author_id = @AuthorId AND revoked_at IS NULL";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@AuthorId", authorId);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    private static RefreshToken MapRefreshToken(NpgsqlDataReader reader)
    {
        return new RefreshToken
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Token = reader.GetString(reader.GetOrdinal("token")),
            AuthorId = reader.GetInt32(reader.GetOrdinal("author_id")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            RevokedAt = GetNullable<DateTime>(reader, "revoked_at"),
            ReplacedByToken = GetNullableString(reader, "replaced_by_token"),
            Author = new Author
            {
                Id = reader.GetInt32(reader.GetOrdinal("author_id_nav")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Bio = GetNullableString(reader, "bio"),
                AvatarUrl = GetNullableString(reader, "avatar_url"),
                CertificateThumbprint = GetNullableString(reader, "certificate_thumbprint"),
                Role = reader.GetString(reader.GetOrdinal("role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("author_created_at")),
                UpdatedAt = GetNullable<DateTime>(reader, "author_updated_at")
            }
        };
    }
}
