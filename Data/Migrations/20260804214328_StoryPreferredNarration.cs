using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoryPreferredNarration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredNarration",
                table: "stories",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredNarration",
                table: "stories");
        }
    }
}
