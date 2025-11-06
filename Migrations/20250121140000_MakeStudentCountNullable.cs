using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentCountNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make StudentCount nullable (it may already be nullable, but ensure it is)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_schema = 'public' 
                        AND table_name = 'Groups' 
                        AND column_name = 'StudentCount'
                    ) THEN
                        -- Update any null values to 0 first (safety check)
                        UPDATE ""Groups"" SET ""StudentCount"" = 0 WHERE ""StudentCount"" IS NULL;
                        
                        -- Make the column nullable
                        ALTER TABLE ""Groups"" ALTER COLUMN ""StudentCount"" DROP NOT NULL;
                        
                        -- Ensure default value is set
                        ALTER TABLE ""Groups"" ALTER COLUMN ""StudentCount"" SET DEFAULT 0;
                    END IF;
                EXCEPTION WHEN OTHERS THEN
                    -- Ignore errors
                    NULL;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: Make StudentCount NOT NULL again
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_schema = 'public' 
                        AND table_name = 'Groups' 
                        AND column_name = 'StudentCount'
                    ) THEN
                        -- Update any null values to 0
                        UPDATE ""Groups"" SET ""StudentCount"" = 0 WHERE ""StudentCount"" IS NULL;
                        
                        -- Make the column NOT NULL
                        ALTER TABLE ""Groups"" ALTER COLUMN ""StudentCount"" SET NOT NULL;
                    END IF;
                EXCEPTION WHEN OTHERS THEN
                    -- Ignore errors
                    NULL;
                END $$;
            ");
        }
    }
}

