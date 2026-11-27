using Npgsql;

namespace SharpQuill.Api.Tests.Fixtures;

/// <summary>
/// Where the integration tests find PostgreSQL, and the one connection pool every test host shares.
/// </summary>
/// <remarks>
/// Resolution order for the connection string:
/// 1. <c>ConnectionStrings__DefaultConnection</c> (CI sets this).
/// 2. The repository's dev <c>.env</c> (<c>DB_PORT</c>, <c>POSTGRES_USER</c>, <c>POSTGRES_PASSWORD</c>), so
///    <c>dotnet test</c> works against <c>docker compose up</c> without extra setup.
/// 3. The CI defaults on port 5432.
/// The database is always <c>cms_test</c>, never <c>POSTGRES_DB</c>: the fixture truncates every table.
/// </remarks>
public static class TestDatabase
{
    private const string DatabaseName = "cms_test";

    // Requests within the "Database" collection run one at a time, but every running host holds one LISTEN
    // connection (SuggestionIndexListener) and a request can use a few more. Generous, and far below the
    // server's max_connections, so a leak fails loudly here instead of starving the dev stack.
    private const int MaxPoolSize = 30;

    public static string ConnectionString { get; } = Resolve();

    /// <summary>
    /// The pool every test host and test helper uses. It lives for the whole test run: the test hosts register
    /// it as an instance, which the DI container never disposes, so a per-host pool would leak its connections.
    /// </summary>
    public static NpgsqlDataSource DataSource { get; } = NpgsqlDataSource.Create(
        new NpgsqlConnectionStringBuilder(ConnectionString) { MaxPoolSize = MaxPoolSize }.ConnectionString);

    private static string Resolve()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var dotEnv = ReadRepositoryDotEnv();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = int.TryParse(dotEnv.GetValueOrDefault("DB_PORT"), out var port) ? port : 5432,
            Database = DatabaseName,
            Username = dotEnv.GetValueOrDefault("POSTGRES_USER") ?? "cms_user",
            Password = dotEnv.GetValueOrDefault("POSTGRES_PASSWORD") ?? "cms_password",
        };
        return builder.ConnectionString;
    }

    /// <summary>Reads KEY=VALUE lines from the .env next to docker-compose.yml, if there is one.</summary>
    private static Dictionary<string, string> ReadRepositoryDotEnv()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
            {
                continue;
            }

            var path = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(path))
            {
                break;
            }

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var value = line[(eq + 1)..].Trim();
                if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                {
                    value = value[1..^1];
                }

                values[line[..eq].Trim()] = value;
            }

            break;
        }

        return values;
    }
}
