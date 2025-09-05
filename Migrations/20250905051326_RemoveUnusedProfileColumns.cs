using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the unused columns from Profiles table
            migrationBuilder.DropColumn(
                name: "InterviewCatalogResponse",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PotentialCareerPaths",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ApiResponseJson",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the columns if rolling back
            migrationBuilder.AddColumn<string>(
                name: "ApiResponseJson",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PotentialCareerPaths",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewCatalogResponse",
                table: "Profiles",
                type: "text",
                nullable: true);
        }
    }
}
