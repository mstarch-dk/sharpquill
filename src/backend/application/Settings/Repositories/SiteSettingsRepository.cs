using Npgsql;

using SharpQuill.Application.Common.Repositories;
using SharpQuill.Core.Settings;

namespace SharpQuill.Application.Settings.Repositories;

public class SiteSettingsRepository : BaseRepository, ISiteSettingsRepository
{
    public SiteSettingsRepository(NpgsqlDataSource dataSource) : base(dataSource)
    {
    }

    public async Task<IEnumerable<SiteSettings>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, key, value, updated_at
            FROM site_settings
            ORDER BY key";

        return await QueryAsync(sql, MapSiteSettings);
    }

    public async Task<SiteSettings?> GetByKeyAsync(string key)
    {
        const string sql = @"
            SELECT id, key, value, updated_at
            FROM site_settings
            WHERE key = @Key";

        return await QuerySingleOrDefaultAsync(sql, MapSiteSettings, new NpgsqlParameter("@Key", key));
    }

    public async Task<SiteSettings> UpsertAsync(string key, string? value)
    {
        const string sql = @"
            INSERT INTO site_settings (key, value, updated_at)
            VALUES (@Key, @Value, @UpdatedAt)
            ON CONFLICT (key)
            DO UPDATE SET value = @Value, updated_at = @UpdatedAt
            RETURNING id, key, value, updated_at";

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", (object?)value ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapSiteSettings(reader);
    }

    public async Task UpsertManyAsync(IDictionary<string, string?> settings)
    {
        if (!settings.Any()) return;

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var (key, value) in settings)
            {
                const string sql = @"
                    INSERT INTO site_settings (key, value, updated_at)
                    VALUES (@Key, @Value, @UpdatedAt)
                    ON CONFLICT (key)
                    DO UPDATE SET value = @Value, updated_at = @UpdatedAt";

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@Key", key);
                command.Parameters.AddWithValue("@Value", (object?)value ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static SiteSettings MapSiteSettings(NpgsqlDataReader reader)
    {
        return new SiteSettings
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Key = reader.GetString(reader.GetOrdinal("key")),
            Value = GetNullableString(reader, "value"),
            UpdatedAt = GetNullable<DateTime>(reader, "updated_at")
        };
    }
}
