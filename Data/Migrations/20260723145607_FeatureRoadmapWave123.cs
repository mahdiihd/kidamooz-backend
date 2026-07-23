using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeatureRoadmapWave123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChallengeTag",
                table: "story_drafts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverPrompt",
                table: "story_drafts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreateStreak",
                table: "app_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastCreateDate",
                table: "app_users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastListenDate",
                table: "app_users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastPlayedPositionSeconds",
                table: "app_users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPlayedStoryId",
                table: "app_users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ListenStreak",
                table: "app_users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "app_users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "free");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlusExpiresAt",
                table: "app_users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "child_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    AvatarKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_child_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_child_profiles_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_favorites",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StoryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_favorites", x => new { x.UserId, x.StoryId });
                    table.ForeignKey(
                        name: "FK_member_favorites_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_member_favorites_stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stories_of_the_day",
                columns: table => new
                {
                    PickDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StoryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stories_of_the_day", x => x.PickDate);
                    table.ForeignKey(
                        name: "FK_stories_of_the_day_stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TitleFa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ThemeTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DescriptionFa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_challenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_child_profiles_UserId",
                table: "child_profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_member_favorites_StoryId",
                table: "member_favorites",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_stories_of_the_day_StoryId",
                table: "stories_of_the_day",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_challenges_IsActive_WeekStart",
                table: "weekly_challenges",
                columns: new[] { "IsActive", "WeekStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "child_profiles");

            migrationBuilder.DropTable(
                name: "member_favorites");

            migrationBuilder.DropTable(
                name: "stories_of_the_day");

            migrationBuilder.DropTable(
                name: "weekly_challenges");

            migrationBuilder.DropColumn(
                name: "ChallengeTag",
                table: "story_drafts");

            migrationBuilder.DropColumn(
                name: "CoverPrompt",
                table: "story_drafts");

            migrationBuilder.DropColumn(
                name: "CreateStreak",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LastCreateDate",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LastListenDate",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LastPlayedPositionSeconds",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LastPlayedStoryId",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "ListenStreak",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "PlusExpiresAt",
                table: "app_users");
        }
    }
}
