using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareerGoals",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strengths",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weaknesses",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareerGoals",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Interests",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Strengths",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Weaknesses",
                table: "Profiles");
        }
    }
}
