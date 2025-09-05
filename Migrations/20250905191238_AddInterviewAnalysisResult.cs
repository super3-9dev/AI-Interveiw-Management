using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAnalysisResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewSessionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Recommendations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MBAFocusArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClarityScore = table.Column<int>(type: "integer", nullable: false),
                    ShortTermRoadmap = table.Column<string>(type: "text", nullable: true),
                    MediumTermRoadmap = table.Column<string>(type: "text", nullable: true),
                    LongTermRoadmap = table.Column<string>(type: "text", nullable: true),
                    AdditionalTips = table.Column<string>(type: "text", nullable: true),
                    RawApiResponse = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAnalysisResults_InterviewSessions_InterviewSession~",
                        column: x => x.InterviewSessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewAnalysisResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAnalysisResults_InterviewSessionId",
                table: "InterviewAnalysisResults",
                column: "InterviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAnalysisResults_UserId",
                table: "InterviewAnalysisResults",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAnalysisResults");
        }
    }
}
