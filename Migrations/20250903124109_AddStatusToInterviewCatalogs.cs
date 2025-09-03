using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToInterviewCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InterviewCatalogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "InterviewCatalogs");
        }
    }
}
