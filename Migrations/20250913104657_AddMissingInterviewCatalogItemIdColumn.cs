using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingInterviewCatalogItemIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing InterviewCatalogItemId column to InterviewSessions table
            migrationBuilder.AddColumn<int>(
                name: "InterviewCatalogItemId",
                table: "InterviewSessions",
                type: "integer",
                nullable: true);

            // Create InterviewCatalogItems table if it doesn't exist
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

            // Add foreign key constraint for InterviewCatalogItemId
            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogItemId",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId",
                principalTable: "InterviewCatalogItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogItems_UserId",
                table: "InterviewCatalogItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogItemId",
                table: "InterviewSessions");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewCatalogItems_UserId",
                table: "InterviewCatalogItems");

            // Drop InterviewCatalogItems table
            migrationBuilder.DropTable(
                name: "InterviewCatalogItems");

            // Drop InterviewCatalogItemId column
            migrationBuilder.DropColumn(
                name: "InterviewCatalogItemId",
                table: "InterviewSessions");
        }
    }
}
