using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Refresh and login tokens are now stored as SHA-256 hashes. Existing rows hold plaintext values that
    /// can no longer be matched, so they are removed: users sign in again and unused magic links are re-issued.
    /// Only the three token tables are touched; no authors, articles or other content.
    /// </summary>
    [DbContext(typeof(SharpQuillDbContext))]
    [Migration("20261018100000_HashStoredTokens")]
    public partial class HashStoredTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM refresh_tokens;");
            migrationBuilder.Sql("DELETE FROM website_refresh_tokens;");
            migrationBuilder.Sql("DELETE FROM website_login_tokens;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted tokens cannot be restored; nothing to undo.
        }
    }
}
