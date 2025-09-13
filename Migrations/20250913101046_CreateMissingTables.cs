using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class CreateMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create InterviewSessions table
            migrationBuilder.CreateTable(
                name: "InterviewSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CandidateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CandidateEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CandidateEducation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CandidateExperience = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentQuestionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PauseReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResumeNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InterviewCatalogId = table.Column<int>(type: "integer", nullable: true),
                    CustomInterviewId = table.Column<int>(type: "integer", nullable: true),
                    AIAgentRoleId = table.Column<int>(type: "integer", nullable: true),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    InterviewCatalogItemId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSessions_AIAgentRoles_AIAgentRoleId",
                        column: x => x.AIAgentRoleId,
                        principalTable: "AIAgentRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InterviewSessions_InterviewCatalogs_InterviewCatalogId",
                        column: x => x.InterviewCatalogId,
                        principalTable: "InterviewCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InterviewSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create InterviewCatalogItems table
            migrationBuilder.CreateTable(
                name: "InterviewCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Instruction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewCatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewCatalogItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create CustomInterviews table
            migrationBuilder.CreateTable(
                name: "CustomInterviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            // Add foreign key for CustomInterviewId in InterviewSessions
            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_CustomInterviews_CustomInterviewId",
                table: "InterviewSessions",
                column: "CustomInterviewId",
                principalTable: "CustomInterviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Add foreign key for InterviewCatalogItemId in InterviewSessions
            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogItemId",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId",
                principalTable: "InterviewCatalogItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create indexes
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
                name: "IX_InterviewSessions_UserId",
                table: "InterviewSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogItems_UserId",
                table: "InterviewCatalogItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomInterviews_UserId",
                table: "CustomInterviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_CustomInterviews_CustomInterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogItemId",
                table: "InterviewSessions");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_AIAgentRoleId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_CustomInterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewCatalogId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_UserId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewCatalogItems_UserId",
                table: "InterviewCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_CustomInterviews_UserId",
                table: "CustomInterviews");

            // Drop tables
            migrationBuilder.DropTable(
                name: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "InterviewCatalogItems");

            migrationBuilder.DropTable(
                name: "CustomInterviews");
        }
    }
}
