using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoryProgressIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProgressIcon",
                table: "stories",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "star");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressIcon",
                table: "stories");
        }
    }
}
