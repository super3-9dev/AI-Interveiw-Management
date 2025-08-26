using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAIAgentRolesAsync(AppDbContext dbContext)
        {
            try
            {
                // Check if AI agent roles already exist
                if (!await dbContext.AIAgentRoles.AnyAsync())
                {
                    var aiAgentRoles = new List<AIAgentRole>
                    {
                        new AIAgentRole
                        {
                            Name = "Career Counselling Interviewer & Analyzer Agent",
                            Description = "Professional career counselor powered by AI that analyzes students' CVs, experiences, and skills, then guides them through structured interviews to identify suitable career paths, job roles, and educational opportunities.",
                            RoleType = "CareerCounselling",
                            Purpose = "To act as a professional career counselor powered by AI. It analyzes students' CVs, experiences, and skills, then guides them through structured interviews to identify suitable career paths, job roles, and educational opportunities.",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        },
                        new AIAgentRole
                        {
                            Name = "Purpose Discovery Interviewer & Reflective Agent",
                            Description = "AI agent that helps students and young professionals explore their deeper sense of direction — what motivates them, what values they hold, and what impact they want to create in their lives and careers.",
                            RoleType = "PurposeDiscovery",
                            Purpose = "To help students and young professionals explore their deeper sense of direction — what motivates them, what values they hold, and what impact they want to create in their lives and careers.",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        }
                    };

                    dbContext.AIAgentRoles.AddRange(aiAgentRoles);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the application startup
                Console.WriteLine($"Error seeding AI agent roles: {ex.Message}");
            }
        }
    }
}
