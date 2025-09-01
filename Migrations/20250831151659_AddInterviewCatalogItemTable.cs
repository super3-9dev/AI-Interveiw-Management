using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewCatalogItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewCatalogItemId",
                table: "InterviewSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCatalogItems_UserId",
                table: "InterviewCatalogItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogIte~",
                table: "InterviewSessions",
                column: "InterviewCatalogItemId",
                principalTable: "InterviewCatalogItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_InterviewCatalogItems_InterviewCatalogIte~",
                table: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "InterviewCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewCatalogItemId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "InterviewCatalogItemId",
                table: "InterviewSessions");
        }
    }
}
