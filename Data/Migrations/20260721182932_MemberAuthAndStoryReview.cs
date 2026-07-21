using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberAuthAndStoryReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "story_drafts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "story_drafts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "story_drafts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "stories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorUserId",
                table: "stories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "app_users",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "app_users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "app_users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "app_users",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.CreateIndex(
                name: "IX_story_drafts_UserId_UpdatedAt",
                table: "story_drafts",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_Mobile",
                table: "app_users",
                column: "Mobile",
                unique: true,
                filter: "[Mobile] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_story_drafts_app_users_UserId",
                table: "story_drafts",
                column: "UserId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_story_drafts_app_users_UserId",
                table: "story_drafts");

            migrationBuilder.DropIndex(
                name: "IX_story_drafts_UserId_UpdatedAt",
                table: "story_drafts");

            migrationBuilder.DropIndex(
                name: "IX_app_users_Mobile",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "story_drafts");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "story_drafts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "story_drafts");

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "AuthorUserId",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "app_users");
        }
    }
}
