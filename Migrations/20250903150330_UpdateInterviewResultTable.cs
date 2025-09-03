using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInterviewResultTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Evaluation",
                table: "InterviewResults");

            migrationBuilder.AddColumn<string>(
                name: "AreasForImprovement",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assessment",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "InterviewResults",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "InterviewResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpQuestion",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "InterviewResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyStrengths",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallEvaluation",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionsAsked",
                table: "InterviewResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "InterviewResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "InterviewResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InterviewResults",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreasForImprovement",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Assessment",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "FollowUpQuestion",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "InterviewType",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "KeyStrengths",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "OverallEvaluation",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "QuestionsAsked",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InterviewResults");

            migrationBuilder.AddColumn<string>(
                name: "Evaluation",
                table: "InterviewResults",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
