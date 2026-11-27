using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNamespaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_refresh_tokens_authors_author_id",
                table: "refresh_tokens");

            migrationBuilder.AddForeignKey(
                name: "f_k_refresh_tokens__authors_author_id",
                table: "refresh_tokens",
                column: "author_id",
                principalTable: "authors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_refresh_tokens__authors_author_id",
                table: "refresh_tokens");

            migrationBuilder.AddForeignKey(
                name: "f_k_refresh_tokens_authors_author_id",
                table: "refresh_tokens",
                column: "author_id",
                principalTable: "authors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
