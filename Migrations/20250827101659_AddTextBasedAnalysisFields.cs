using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTextBasedAnalysisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareerGoals",
                table: "ResumeAnalyses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentActivity",
                table: "ResumeAnalyses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ResumeAnalyses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "ResumeAnalyses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Motivations",
                table: "ResumeAnalyses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareerGoals",
                table: "ResumeAnalyses");

            migrationBuilder.DropColumn(
                name: "CurrentActivity",
                table: "ResumeAnalyses");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ResumeAnalyses");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "ResumeAnalyses");

            migrationBuilder.DropColumn(
                name: "Motivations",
                table: "ResumeAnalyses");
        }
    }
}
