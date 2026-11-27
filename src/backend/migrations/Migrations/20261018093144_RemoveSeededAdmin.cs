using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededAdmin : Migration
    {
        /// <summary>
        /// The seeded admin@example.com account had a publicly known password. EF generated an unconditional
        /// DeleteData for author 1, which would delete the real admin of existing installations (their author 1
        /// has a changed password and owns content). This only removes the account when it still carries the
        /// known default hash and nothing references it, i.e. on fresh/unused databases.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM authors a
                WHERE a.id = 1
                  AND a.email = 'admin@example.com'
                  AND a.password_hash = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.VTtYE9rikCKK5.'
                  AND NOT EXISTS (SELECT 1 FROM articles WHERE author_id = a.id)
                  AND NOT EXISTS (SELECT 1 FROM media WHERE author_id = a.id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the seeded default admin is not restored.
        }
    }
}
