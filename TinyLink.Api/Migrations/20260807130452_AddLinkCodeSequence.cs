using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyLink.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkCodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "link_code_req",
                minValue: 1L,
                maxValue: 3521614606207L,
                oldMinValue: 1L,
                oldMaxValue: 56L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "link_code_req",
                minValue: 1L,
                maxValue: 56L,
                oldMinValue: 1L,
                oldMaxValue: 3521614606207L);
        }
    }
}
