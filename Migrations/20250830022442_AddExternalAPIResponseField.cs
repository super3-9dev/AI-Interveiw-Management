using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalAPIResponseField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalAPIResponse",
                table: "Profiles",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalAPIResponse",
                table: "Profiles");
        }
    }
}
