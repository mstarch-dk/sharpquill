using Npgsql;
using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Auth;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Repositories;

public class WebsiteRefreshTokenRepository : BaseRepository, IWebsiteRefreshTokenRepository
{
    public WebsiteRefreshTokenRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<WebsiteRefreshToken?> GetByTokenAsync(string token)
    {
        const string sql = @"
            SELECT rt.id, rt.token, rt.website_user_id, rt.expires_at, rt.created_at, rt.revoked_at, rt.replaced_by_token,
                   wu.id AS user_id, wu.name, wu.email, wu.status, wu.created_at AS user_created_at, wu.updated_at AS user_updated_at
            FROM website_refresh_tokens rt
            INNER JOIN website_users wu ON rt.website_user_id = wu.id
            WHERE rt.token = @Token";

        return await QuerySingleOrDefaultAsync(sql, MapRefreshToken,
            new NpgsqlParameter("@Token", TokenHasher.Hash(token)));
    }

    public async Task<WebsiteRefreshToken> CreateAsync(WebsiteRefreshToken refreshToken)
    {
        const string sql = @"
            INSERT INTO website_refresh_tokens (token, website_user_id, expires_at, created_at)
            VALUES (@Token, @WebsiteUserId, @ExpiresAt, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Token", TokenHasher.Hash(refreshToken.Token));
        command.Parameters.AddWithValue("@WebsiteUserId", refreshToken.WebsiteUserId);
        command.Parameters.AddWithValue("@ExpiresAt", refreshToken.ExpiresAt);
        command.Parameters.AddWithValue("@CreatedAt", refreshToken.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        refreshToken.Id = Convert.ToInt32(id);

        return refreshToken;
    }

    public async Task<bool> RevokeAsync(int id, string? replacedByToken = null)
    {
        const string sql = @"
            UPDATE website_refresh_tokens
            SET revoked_at = @RevokedAt, replaced_by_token = @ReplacedByToken
            WHERE id = @Id AND revoked_at IS NULL";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@ReplacedByToken", replacedByToken == null ? DBNull.Value : TokenHasher.Hash(replacedByToken));

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task RevokeAllForUserAsync(int websiteUserId)
    {
        const string sql = @"
            UPDATE website_refresh_tokens
            SET revoked_at = @RevokedAt
            WHERE website_user_id = @WebsiteUserId AND revoked_at IS NULL";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@WebsiteUserId", websiteUserId);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    private static WebsiteRefreshToken MapRefreshToken(NpgsqlDataReader reader)
    {
        return new WebsiteRefreshToken
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Token = reader.GetString(reader.GetOrdinal("token")),
            WebsiteUserId = reader.GetInt32(reader.GetOrdinal("website_user_id")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            RevokedAt = GetNullable<DateTime>(reader, "revoked_at"),
            ReplacedByToken = GetNullableString(reader, "replaced_by_token"),
            WebsiteUser = new WebsiteUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Status = (WebsiteUserStatus)reader.GetInt32(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("user_created_at")),
                UpdatedAt = GetNullable<DateTime>(reader, "user_updated_at")
            }
        };
    }
}
