using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInterviewSessionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_SubTopics_SubTopicId",
                table: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "SubTopics");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_SubTopicId",
                table: "InterviewSessions");

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

            migrationBuilder.RenameColumn(
                name: "SubTopicId",
                table: "InterviewSessions",
                newName: "Type");

            migrationBuilder.AddColumn<int>(
                name: "AIAgentRoleId",
                table: "InterviewSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomInterviewId",
                table: "InterviewSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewCatalogId",
                table: "InterviewSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PauseReason",
                table: "InterviewSessions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "InterviewSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeNotes",
                table: "InterviewSessions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumedAt",
                table: "InterviewSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "InterviewSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomInterviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CustomQuestions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FocusAreas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DifficultyLevel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InterviewDuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomInterviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomInterviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AIAgentRoleId = table.Column<int>(type: "integer", nullable: false),
                    InterviewStructure = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KeyQuestions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewCatalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewCatalogs_AIAgentRoles_AIAgentRoleId",
                        column: x => x.AIAgentRoleId,
                        principalTable: "AIAgentRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewCatalogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_AIAgentRoleId",
                table: "InterviewSessions",
                column: "AIAgentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_CustomInterviewId",
                table: "InterviewSessions",
                column: "CustomInterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewCatalogId",
                table: "InterviewSessions",
                column: "InterviewCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomInterviews_UserId",
                table: "CustomInterviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogs_AIAgentRoleId",
                table: "InterviewCatalogs",
                column: "AIAgentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogs_UserId",
                table: "InterviewCatalogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_AIAgentRoles_AIAgentRoleId",
                table: "InterviewSessions",
                column: "AIAgentRoleId",
                principalTable: "AIAgentRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_CustomInterviews_CustomInterviewId",
                table: "InterviewSessions",
                column: "CustomInterviewId",
                principalTable: "CustomInterviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogs_InterviewCatalogId",
                table: "InterviewSessions",
                column: "InterviewCatalogId",
                principalTable: "InterviewCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_AIAgentRoles_AIAgentRoleId",
                table: "InterviewSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_CustomInterviews_CustomInterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogs_InterviewCatalogId",
                table: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "CustomInterviews");

            migrationBuilder.DropTable(
                name: "InterviewCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_AIAgentRoleId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_CustomInterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewCatalogId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "AIAgentRoleId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "CustomInterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "InterviewCatalogId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "PauseReason",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "ResumeNotes",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "ResumedAt",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InterviewSessions");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "InterviewSessions",
                newName: "SubTopicId");

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

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Objectives = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TopicId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CandidateEmail = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubTopics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_SubTopicId",
                table: "InterviewSessions",
                column: "SubTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTopics_TopicId",
                table: "SubTopics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTopics_UserId",
                table: "SubTopics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_UserId",
                table: "Topics",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_SubTopics_SubTopicId",
                table: "InterviewSessions",
                column: "SubTopicId",
                principalTable: "SubTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
