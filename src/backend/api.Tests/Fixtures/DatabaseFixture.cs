using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace SharpQuill.Api.Tests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    public static string ConnectionString => TestDatabase.ConnectionString;

    public TestWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Ensure the test database exists
        await EnsureDatabaseExistsAsync();

        // Apply EF Core migrations
        var options = new DbContextOptionsBuilder<SharpQuillDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new SharpQuillDbContext(options);
        await context.Database.MigrateAsync();

        // Truncate all tables and reseed
        await TruncateAndSeedAsync();

        // Create the factory
        Factory = new TestWebApplicationFactory();
    }

    public Task DisposeAsync()
    {
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task EnsureDatabaseExistsAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            await using var createCmd = conn.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task TruncateAndSeedAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var conn = await dataSource.OpenConnectionAsync();

        // Truncate all tables
        await using (var truncateCmd = conn.CreateCommand())
        {
            truncateCmd.CommandText = @"
                TRUNCATE TABLE
                    article_tags,
                    article_contents,
                    refresh_tokens,
                    website_login_tokens,
                    website_refresh_tokens,
                    navigation_items,
                    navigations,
                    articles,
                    categories,
                    tags,
                    series,
                    media,
                    site_settings,
                    website_users,
                    authors
                RESTART IDENTITY CASCADE;";
            await truncateCmd.ExecuteNonQueryAsync();
        }

        // Seed test data
        await using (var seedCmd = conn.CreateCommand())
        {
            seedCmd.CommandText = TestSeedData.SeedSql;
            await seedCmd.ExecuteNonQueryAsync();
        }

        // Ensure seeded credentials always match test constants even if a static hash drifts.
        await using (var passwordCmd = conn.CreateCommand())
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestSeedData.AdminPassword, workFactor: 12);
            passwordCmd.CommandText = @"
                UPDATE authors
                SET password_hash = @PasswordHash
                WHERE email = @Email;";
            passwordCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            passwordCmd.Parameters.AddWithValue("@Email", TestSeedData.AdminEmail);
            await passwordCmd.ExecuteNonQueryAsync();
        }

        // Reset sequences to avoid ID conflicts
        await using (var seqCmd = conn.CreateCommand())
        {
            seqCmd.CommandText = @"
                SELECT setval('authors_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM authors), 1));
                SELECT setval('categories_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM categories), 1));
                SELECT setval('tags_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM tags), 1));
                SELECT setval('series_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM series), 1));
                SELECT setval('articles_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM articles), 1));
                SELECT setval('article_contents_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM article_contents), 1));
                SELECT setval('navigations_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM navigations), 1));
                SELECT setval('navigation_items_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM navigation_items), 1));
                SELECT setval('media_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM media), 1));
                SELECT setval('site_settings_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM site_settings), 1));
                SELECT setval('refresh_tokens_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM refresh_tokens), 1));
                SELECT setval('website_users_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM website_users), 1));
                SELECT setval('website_login_tokens_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM website_login_tokens), 1));
                SELECT setval('website_refresh_tokens_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM website_refresh_tokens), 1));";
            await seqCmd.ExecuteNonQueryAsync();
        }
    }
}
