using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExternalAPIResponseAndCurrentStepDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStepDescription",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ExternalAPIResponse",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FutureCareerGoals",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PotentialCareerPaths",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentStepDescription",
                table: "Profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAPIResponse",
                table: "Profiles",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FutureCareerGoals",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PotentialCareerPaths",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
