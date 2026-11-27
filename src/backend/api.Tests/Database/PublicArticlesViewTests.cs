using FluentAssertions;
using Npgsql;
using Xunit;

using SharpQuill.Api.Tests.Fixtures;

namespace SharpQuill.Api.Tests.Database;

/// <summary>
/// The public_articles view (migration AddPublicArticlesView) is "SELECT a.* FROM articles a WHERE ...", and Postgres
/// freezes "a.*" when the view is created. A migration that adds a column to articles must therefore recreate the
/// view, or public queries silently miss the new column. This test catches that.
/// </summary>
[Collection("Database")]
public class PublicArticlesViewTests
{
    // Constructor parameter makes xUnit run the fixture (migrations) before this test
    public PublicArticlesViewTests(DatabaseFixture fixture) { _ = fixture; }

    [Fact]
    public async Task PublicArticlesView_HasTheSameColumnsAsArticles()
    {
        (await ColumnsAsync("public_articles")).Should().Equal(await ColumnsAsync("articles"),
            "a migration that changes the articles columns must re-run CREATE OR REPLACE VIEW public_articles");
    }

    private static async Task<List<string>> ColumnsAsync(string table)
    {
        await using var conn = new NpgsqlConnection(DatabaseFixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = current_schema() AND table_name = @table
            ORDER BY ordinal_position", conn);
        cmd.Parameters.AddWithValue("@table", table);

        var columns = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(0));
        return columns;
    }
}
