using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveResultIdFromInterviewSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewResults_InterviewSessions_SessionId",
                table: "InterviewResults");

            migrationBuilder.DropIndex(
                name: "IX_InterviewResults_SessionId",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "AreasForImprovement",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Assessment",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "FollowUpQuestion",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "InterviewType",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "KeyStrengths",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "OverallEvaluation",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "InterviewResults");

            migrationBuilder.RenameColumn(
                name: "QuestionsAsked",
                table: "InterviewResults",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "InterviewResults",
                newName: "CompleteDate");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "InterviewResults",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Question",
                table: "InterviewResults",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewResults_UserId",
                table: "InterviewResults",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewResults_Users_UserId",
                table: "InterviewResults",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewResults_Users_UserId",
                table: "InterviewResults");

            migrationBuilder.DropIndex(
                name: "IX_InterviewResults_UserId",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Question",
                table: "InterviewResults");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "InterviewResults",
                newName: "QuestionsAsked");

            migrationBuilder.RenameColumn(
                name: "CompleteDate",
                table: "InterviewResults",
                newName: "CompletedAt");

            migrationBuilder.AddColumn<string>(
                name: "AreasForImprovement",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assessment",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "InterviewResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpQuestion",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "InterviewResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyStrengths",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallEvaluation",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "InterviewResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "InterviewResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewResults_SessionId",
                table: "InterviewResults",
                column: "SessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewResults_InterviewSessions_SessionId",
                table: "InterviewResults",
                column: "SessionId",
                principalTable: "InterviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
