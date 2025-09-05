using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatMessagesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_InterviewSessions_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsUserMessage",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "ChatMessages",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_UserId");

            migrationBuilder.AddColumn<string>(
                name: "InterviewId",
                table: "ChatMessages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Question",
                table: "ChatMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_UserId",
                table: "ChatMessages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_UserId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "InterviewId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Question",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ChatMessages",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_SessionId");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserMessage",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_InterviewSessions_SessionId",
                table: "ChatMessages",
                column: "SessionId",
                principalTable: "InterviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
