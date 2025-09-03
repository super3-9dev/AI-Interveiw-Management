using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedFieldsFromInterviewCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewCatalogs_AIAgentRoles_AIAgentRoleId",
                table: "InterviewCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_InterviewCatalogs_AIAgentRoleId",
                table: "InterviewCatalogs");

            migrationBuilder.DropColumn(
                name: "AIAgentRoleId",
                table: "InterviewCatalogs");

            migrationBuilder.DropColumn(
                name: "KeyQuestions",
                table: "InterviewCatalogs");

            migrationBuilder.DropColumn(
                name: "TargetSkills",
                table: "InterviewCatalogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AIAgentRoleId",
                table: "InterviewCatalogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KeyQuestions",
                table: "InterviewCatalogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSkills",
                table: "InterviewCatalogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogs_AIAgentRoleId",
                table: "InterviewCatalogs",
                column: "AIAgentRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewCatalogs_AIAgentRoles_AIAgentRoleId",
                table: "InterviewCatalogs",
                column: "AIAgentRoleId",
                principalTable: "AIAgentRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
