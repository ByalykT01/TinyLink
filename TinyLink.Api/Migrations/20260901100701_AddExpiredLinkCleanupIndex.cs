using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyLink.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiredLinkCleanupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Links_ExpiresAt",
                table: "Links",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_ExpiresAt",
                table: "Links");
        }
    }
}
