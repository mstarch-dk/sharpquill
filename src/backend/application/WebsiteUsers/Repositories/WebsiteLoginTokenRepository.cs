using Npgsql;
using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Auth;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Repositories;

public class WebsiteLoginTokenRepository : BaseRepository, IWebsiteLoginTokenRepository
{
    public WebsiteLoginTokenRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<WebsiteLoginToken?> GetByTokenAsync(string token)
    {
        const string sql = @"
            SELECT lt.id, lt.website_user_id, lt.token, lt.expires_at, lt.created_at, lt.used_at,
                   wu.id AS user_id, wu.name, wu.email, wu.status, wu.created_at AS user_created_at, wu.updated_at AS user_updated_at
            FROM website_login_tokens lt
            INNER JOIN website_users wu ON lt.website_user_id = wu.id
            WHERE lt.token = @Token";

        return await QuerySingleOrDefaultAsync(sql, MapLoginToken,
            new NpgsqlParameter("@Token", TokenHasher.Hash(token)));
    }

    public async Task<WebsiteLoginToken> CreateAsync(WebsiteLoginToken loginToken)
    {
        const string sql = @"
            INSERT INTO website_login_tokens (website_user_id, token, expires_at, created_at)
            VALUES (@WebsiteUserId, @Token, @ExpiresAt, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@WebsiteUserId", loginToken.WebsiteUserId);
        command.Parameters.AddWithValue("@Token", TokenHasher.Hash(loginToken.Token));
        command.Parameters.AddWithValue("@ExpiresAt", loginToken.ExpiresAt);
        command.Parameters.AddWithValue("@CreatedAt", loginToken.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        loginToken.Id = Convert.ToInt32(id);

        return loginToken;
    }

    public async Task<bool> MarkUsedAsync(int id)
    {
        const string sql = @"
            UPDATE website_login_tokens
            SET used_at = @UsedAt
            WHERE id = @Id AND used_at IS NULL";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@UsedAt", DateTime.UtcNow);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static WebsiteLoginToken MapLoginToken(NpgsqlDataReader reader)
    {
        return new WebsiteLoginToken
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            WebsiteUserId = reader.GetInt32(reader.GetOrdinal("website_user_id")),
            Token = reader.GetString(reader.GetOrdinal("token")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UsedAt = GetNullable<DateTime>(reader, "used_at"),
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
