using Npgsql;
using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Common;
using SharpQuill.Core.WebsiteUsers;

namespace SharpQuill.Application.WebsiteUsers.Repositories;

public class AdminWebsiteUserRepository : BaseRepository, IAdminWebsiteUserRepository
{
    public AdminWebsiteUserRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<(IEnumerable<WebsiteUser> Users, int TotalCount)> GetAllAsync(PaginationParams pagination, WebsiteUserStatus? status = null)
    {
        var pageSize = pagination.GetValidatedPageSize();
        var page = pagination.GetValidatedPage();
        var offset = (page - 1) * pageSize;

        var whereClause = status.HasValue ? "WHERE status = @Status" : "";

        var countSql = $"SELECT COUNT(*) FROM website_users {whereClause}";
        var dataSql = $@"
            SELECT id, name, email, status, created_at, updated_at
            FROM website_users
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var parameters = new List<NpgsqlParameter>
        {
            new("@Limit", pageSize),
            new("@Offset", offset)
        };

        if (status.HasValue)
        {
            parameters.Add(new NpgsqlParameter("@Status", (int)status.Value));
        }

        var totalCount = await ExecuteScalarIntAsync(countSql,
            status.HasValue ? [new NpgsqlParameter("@Status", (int)status.Value)] : []);

        var users = await QueryAsync(dataSql, MapWebsiteUser, parameters.ToArray());

        return (users, totalCount);
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

    public async Task UpdateStatusAsync(int id, WebsiteUserStatus status)
    {
        const string sql = @"
            UPDATE website_users
            SET status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Status", (int)status);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<(int Pending, int Approved, int Rejected)> GetCountsByStatusAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*) FILTER (WHERE status = 0) AS pending,
                COUNT(*) FILTER (WHERE status = 1) AS approved,
                COUNT(*) FILTER (WHERE status = 2) AS rejected
            FROM website_users";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return (
                reader.GetInt32(reader.GetOrdinal("pending")),
                reader.GetInt32(reader.GetOrdinal("approved")),
                reader.GetInt32(reader.GetOrdinal("rejected"))
            );
        }

        return (0, 0, 0);
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
