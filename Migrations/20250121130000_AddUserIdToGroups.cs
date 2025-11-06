using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewBot.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UserId column if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name = 'Groups' 
                        AND column_name = 'UserId'
                    ) THEN
                        ALTER TABLE ""Groups"" ADD COLUMN ""UserId"" integer NOT NULL DEFAULT 1;
                        
                        -- Add foreign key constraint
                        ALTER TABLE ""Groups""
                        ADD CONSTRAINT ""FK_Groups_Users_UserId""
                        FOREIGN KEY (""UserId"")
                        REFERENCES ""Users"" (""Id"")
                        ON DELETE CASCADE;
                        
                        -- Remove default after adding the column
                        ALTER TABLE ""Groups"" ALTER COLUMN ""UserId"" DROP DEFAULT;
                    END IF;
                END $$;
            ");

            // Fix StudentCount column - set default value for null records and make it NOT NULL
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    -- Update any null StudentCount values to 0
                    UPDATE ""Groups"" SET ""StudentCount"" = 0 WHERE ""StudentCount"" IS NULL;
                    
                    -- Set default value for StudentCount if not already set
                    ALTER TABLE ""Groups"" ALTER COLUMN ""StudentCount"" SET DEFAULT 0;
                    
                    -- Make StudentCount NOT NULL if it's currently nullable
                    ALTER TABLE ""Groups"" ALTER COLUMN ""StudentCount"" SET NOT NULL;
                END $$;
            ");

            // Add CreatedAt and UpdatedAt columns if they don't exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name = 'Groups' 
                        AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE ""Groups"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
                    END IF;
                    
                    IF NOT EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name = 'Groups' 
                        AND column_name = 'UpdatedAt'
                    ) THEN
                        ALTER TABLE ""Groups"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key constraint
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 
                        FROM information_schema.table_constraints 
                        WHERE table_name = 'Groups' 
                        AND constraint_name = 'FK_Groups_Users_UserId'
                    ) THEN
                        ALTER TABLE ""Groups"" DROP CONSTRAINT ""FK_Groups_Users_UserId"";
                    END IF;
                END $$;
            ");

            // Remove columns
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Groups");
        }
    }
}

