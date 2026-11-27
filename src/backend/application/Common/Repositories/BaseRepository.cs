using Npgsql;

namespace SharpQuill.Application.Common.Repositories;

public abstract class BaseRepository
{
    protected readonly NpgsqlDataSource DataSource;

    protected BaseRepository(NpgsqlDataSource dataSource)
    {
        DataSource = dataSource;
    }

    protected async Task<List<T>> QueryAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<T>();

        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }

    protected async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters) where T : class
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return map(reader);
        }

        return null;
    }

    protected async Task<int> ExecuteScalarIntAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    protected static T? GetNullable<T>(NpgsqlDataReader reader, string column) where T : struct
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
    }

    protected static string? GetNullableString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
