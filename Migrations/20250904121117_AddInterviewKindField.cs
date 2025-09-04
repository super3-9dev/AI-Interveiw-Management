using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewKindField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewKind",
                table: "InterviewCatalogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewKind",
                table: "InterviewCatalogs");
        }
    }
}
