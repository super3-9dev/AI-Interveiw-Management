using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class ChangeInterviewIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewResults_InterviewCatalogs_InterviewId",
                table: "InterviewResults");

            migrationBuilder.DropIndex(
                name: "IX_InterviewResults_InterviewId",
                table: "InterviewResults");

            migrationBuilder.AlterColumn<string>(
                name: "InterviewId",
                table: "InterviewResults",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "InterviewId",
                table: "InterviewResults",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewResults_InterviewId",
                table: "InterviewResults",
                column: "InterviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewResults_InterviewCatalogs_InterviewId",
                table: "InterviewResults",
                column: "InterviewId",
                principalTable: "InterviewCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
