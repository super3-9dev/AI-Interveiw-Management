using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddNullableInterviewIdToInterviewResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewId",
                table: "InterviewResults",
                type: "integer",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewResults_InterviewCatalogs_InterviewId",
                table: "InterviewResults");

            migrationBuilder.DropIndex(
                name: "IX_InterviewResults_InterviewId",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "InterviewId",
                table: "InterviewResults");
        }
    }
}
