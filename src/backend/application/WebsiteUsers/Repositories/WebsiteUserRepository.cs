using Npgsql;
using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Repositories;

public class WebsiteUserRepository : BaseRepository, IWebsiteUserRepository
{
    public WebsiteUserRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<WebsiteUser?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id, name, email, status, created_at, updated_at
            FROM website_users
            WHERE email = @Email";

        return await QuerySingleOrDefaultAsync(sql, MapWebsiteUser,
            new NpgsqlParameter("@Email", email));
    }

    public async Task<WebsiteUser?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, name, email, status, created_at, updated_at
            FROM website_users
            WHERE id = @Id";

        return await QuerySingleOrDefaultAsync(sql, MapWebsiteUser,
            new NpgsqlParameter("@Id", id));
    }

    public async Task<WebsiteUser> CreateAsync(WebsiteUser user)
    {
        const string sql = @"
            INSERT INTO website_users (name, email, status, created_at)
            VALUES (@Name, @Email, @Status, @CreatedAt)
            RETURNING id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Name", user.Name);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@Status", (int)user.Status);
        command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

        var id = await command.ExecuteScalarAsync();
        user.Id = Convert.ToInt32(id);

        return user;
    }

    private static WebsiteUser MapWebsiteUser(NpgsqlDataReader reader)
    {
        return new WebsiteUser
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            Status = (WebsiteUserStatus)reader.GetInt32(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at")
        };
    }
}
