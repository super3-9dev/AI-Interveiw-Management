using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAIAgentRoleAndUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedAIAgentRoleId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AIAgentRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RoleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAgentRoles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedAIAgentRoleId",
                table: "Users",
                column: "SelectedAIAgentRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AIAgentRoles_SelectedAIAgentRoleId",
                table: "Users",
                column: "SelectedAIAgentRoleId",
                principalTable: "AIAgentRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_AIAgentRoles_SelectedAIAgentRoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AIAgentRoles");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedAIAgentRoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedAIAgentRoleId",
                table: "Users");
        }
    }
}
