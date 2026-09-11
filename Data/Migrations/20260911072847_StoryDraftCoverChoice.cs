using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoryDraftCoverChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverChoice",
                table: "story_drafts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "drawing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverChoice",
                table: "story_drafts");
        }
    }
}
