using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyLink.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedLinkCleanupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Links_DeletedAt",
                table: "Links",
                column: "DeletedAt",
                filter: "\"DeletedAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_DeletedAt",
                table: "Links");
        }
    }
}
