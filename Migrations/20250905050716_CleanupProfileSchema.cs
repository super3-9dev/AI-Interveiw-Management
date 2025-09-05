using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class CleanupProfileSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop columns if they exist
            migrationBuilder.DropColumn(
                name: "ExternalAPIResponse",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "TopicsMarkdown",
                table: "Profiles");

            // Only rename CareerGoals to FutureCareerGoals if FutureCareerGoals doesn't already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'Profiles' AND column_name = 'CareerGoals')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'Profiles' AND column_name = 'FutureCareerGoals')
                    THEN
                        ALTER TABLE ""Profiles"" RENAME COLUMN ""CareerGoals"" TO ""FutureCareerGoals"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only rename FutureCareerGoals to CareerGoals if CareerGoals doesn't already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'Profiles' AND column_name = 'FutureCareerGoals')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name = 'Profiles' AND column_name = 'CareerGoals')
                    THEN
                        ALTER TABLE ""Profiles"" RENAME COLUMN ""FutureCareerGoals"" TO ""CareerGoals"";
                    END IF;
                END $$;
            ");

            migrationBuilder.AddColumn<string>(
                name: "ExternalAPIResponse",
                table: "Profiles",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopicsMarkdown",
                table: "Profiles",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);
        }
    }
}
