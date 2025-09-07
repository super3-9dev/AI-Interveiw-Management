using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddApiAnalysisFieldsToInterviewResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApiCallDate",
                table: "InterviewResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApiCallSuccessful",
                table: "InterviewResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApiErrorMessage",
                table: "InterviewResults",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiRequestPayload",
                table: "InterviewResults",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiResponse",
                table: "InterviewResults",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiCallDate",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "ApiCallSuccessful",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "ApiErrorMessage",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "ApiRequestPayload",
                table: "InterviewResults");

            migrationBuilder.DropColumn(
                name: "ApiResponse",
                table: "InterviewResults");
        }
    }
}
