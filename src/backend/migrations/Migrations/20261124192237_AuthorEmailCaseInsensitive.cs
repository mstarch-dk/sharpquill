using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Author emails become case-insensitive: existing emails are trimmed and lower-cased (the API now stores them
    /// that way) and a unique index on <c>lower(email)</c> stops two accounts for one mailbox. If two existing authors
    /// differ only by case, the migration stops with a message naming them; merge or rename one and run it again.
    /// </summary>
    public partial class AuthorEmailCaseInsensitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DO $$
            DECLARE
                clashes text;
            BEGIN
                SELECT string_agg(emails, '; ') INTO clashes
                FROM (
                    SELECT string_agg(email, ', ' ORDER BY id) AS emails
                    FROM authors
                    GROUP BY lower(trim(email))
                    HAVING COUNT(*) > 1
                ) d;
                IF clashes IS NOT NULL THEN
                    RAISE EXCEPTION 'Authors with emails that differ only by case or spaces: %. Rename or remove one of each before migrating.', clashes;
                END IF;
            END $$;

            UPDATE authors SET email = lower(trim(email)) WHERE email <> lower(trim(email));

            CREATE UNIQUE INDEX ix_authors_email_lower ON authors (lower(email));
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS ix_authors_email_lower;""");
        }
    }
}
