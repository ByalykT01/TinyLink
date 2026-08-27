using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyLink.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "DeleteTokenHash",
                table: "Links",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteTokenHash",
                table: "Links");
        }
    }
}
