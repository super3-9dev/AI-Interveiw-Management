using Microsoft.EntityFrameworkCore;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.Extensions.Logging;

namespace InterviewBot.Services
{
    public interface IInterviewService
    {
        Task<IEnumerable<InterviewCatalog>> GenerateInterviewCatalogsAsync(int profileId, int userId);
        Task<InterviewCatalog> CreateCustomInterviewAsync(string title, string description, string customQuestions, string focusAreas, string difficultyLevel, string interviewDuration, int userId);
        Task<InterviewSession> StartInterviewAsync(int catalogId, int? customInterviewId, InterviewType type, int userId);
        Task<bool> SaveInterviewProgressAsync(int sessionId, int userId);
        Task<bool> FinishInterviewAsync(int sessionId, int userId);
        Task<bool> PauseInterviewAsync(int sessionId, int userId, string pauseReason);
        Task<bool> ResumeInterviewAsync(int sessionId, int userId);
        Task<InterviewSession?> GetInterviewSessionAsync(int sessionId, int userId);
        Task<IEnumerable<InterviewSession>> GetUserInterviewSessionsAsync(int userId);
        Task<IEnumerable<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId);
        Task<IEnumerable<CustomInterview>> GetUserCustomInterviewsAsync(int userId);
    }

    public class InterviewService : IInterviewService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(AppDbContext dbContext, ILogger<InterviewService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Implementation methods will be added here
        public async Task<IEnumerable<InterviewCatalog>> GenerateInterviewCatalogsAsync(int profileId, int userId)
        {
            try
            {
                _logger.LogInformation("Generating interview catalogs for profile {ProfileId} and user {UserId}", profileId, userId);

                // Get the profile to understand the user's background
                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .ThenInclude(u => u.SelectedAIAgentRole)
                    .FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId);

                if (profile == null)
                {
                    throw new ArgumentException("Profile not found");
                }

                var catalogs = new List<InterviewCatalog>();

                // Generate 3 different interview catalogs based on the profile
                var catalog1 = new InterviewCatalog
                {
                    UserId = userId,
                    Title = "Career Path Exploration",
                    Description = "Deep dive into your career aspirations and professional development",
                    InterviewType = "Career Counselling",
                    AIAgentRoleId = profile.User.SelectedAIAgentRole.Id,
                    InterviewStructure = GenerateInterviewStructure("career_exploration"),
                    KeyQuestions = "What are your long-term career goals? How do you see yourself growing professionally?",
                    TargetSkills = "Career Planning, Goal Setting, Professional Development",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var catalog2 = new InterviewCatalog
                {
                    UserId = userId,
                    Title = "Skills Assessment & Gap Analysis",
                    Description = "Evaluate your current skills and identify areas for improvement",
                    InterviewType = "Career Counselling",
                    AIAgentRoleId = profile.User.SelectedAIAgentRole.Id,
                    InterviewStructure = GenerateInterviewStructure("skills_assessment"),
                    KeyQuestions = "What are your strongest technical skills? Where do you see room for improvement?",
                    TargetSkills = "Skills Assessment, Gap Analysis, Learning Planning",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var catalog3 = new InterviewCatalog
                {
                    UserId = userId,
                    Title = "Industry & Market Positioning",
                    Description = "Understand your position in the market and industry opportunities",
                    InterviewType = "Career Counselling",
                    AIAgentRoleId = profile.User.SelectedAIAgentRole.Id,
                    InterviewStructure = GenerateInterviewStructure("market_positioning"),
                    KeyQuestions = "What industry trends interest you most? How do you differentiate yourself?",
                    TargetSkills = "Market Analysis, Industry Knowledge, Personal Branding",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                catalogs.Add(catalog1);
                catalogs.Add(catalog2);
                catalogs.Add(catalog3);

                _dbContext.InterviewCatalogs.AddRange(catalogs);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully generated {Count} interview catalogs", catalogs.Count);
                return catalogs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating interview catalogs for profile {ProfileId}", profileId);
                throw;
            }
        }

        public async Task<InterviewCatalog> CreateCustomInterviewAsync(string title, string description, string customQuestions, string focusAreas, string difficultyLevel, string interviewDuration, int userId)
        {
            try
            {
                _logger.LogInformation("Creating custom interview for user {UserId}", userId);

                var customInterview = new CustomInterview
                {
                    UserId = userId,
                    Title = title,
                    Description = description,
                    CustomQuestions = customQuestions,
                    FocusAreas = focusAreas,
                    DifficultyLevel = difficultyLevel,
                    InterviewDuration = interviewDuration,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.CustomInterviews.Add(customInterview);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created custom interview {InterviewId}", customInterview.Id);
                return new InterviewCatalog
                {
                    UserId = userId,
                    Title = title,
                    Description = description,
                    InterviewType = "Custom",
                    AIAgentRoleId = 1, // Default AI agent role
                    InterviewStructure = GenerateInterviewStructure("custom"),
                    KeyQuestions = customQuestions,
                    TargetSkills = focusAreas,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom interview for user {UserId}", userId);
                throw;
            }
        }

        public async Task<InterviewSession> StartInterviewAsync(int catalogId, int? customInterviewId, InterviewType type, int userId)
        {
            try
            {
                _logger.LogInformation("Starting interview session for user {UserId}", userId);

                var session = new InterviewSession
                {
                    UserId = userId,
                    Type = type,
                    Status = InterviewStatus.InProgress,
                    StartTime = DateTime.UtcNow,
                    InterviewCatalogId = catalogId,
                    CustomInterviewId = customInterviewId,
                    CurrentQuestionNumber = 0,
                    IsCompleted = false,
                    Language = InterviewLanguage.English // Default to English
                };

                _dbContext.InterviewSessions.Add(session);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully started interview session {SessionId}", session.Id);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting interview session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SaveInterviewProgressAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Paused;
                session.PausedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} paused for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving interview progress for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> FinishInterviewAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Completed;
                session.EndTime = DateTime.UtcNow;
                session.IsCompleted = true;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} completed for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> PauseInterviewAsync(int sessionId, int userId, string pauseReason)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Paused;
                session.PausedAt = DateTime.UtcNow;
                session.PauseReason = pauseReason;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} paused for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> ResumeInterviewAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.InProgress;
                session.ResumedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} resumed for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<InterviewSession?> GetInterviewSessionAsync(int sessionId, int userId)
        {
            return await _dbContext.InterviewSessions
                .Include(s => s.InterviewCatalog)
                .Include(s => s.CustomInterview)
                .Include(s => s.AIAgentRole)
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<IEnumerable<InterviewSession>> GetUserInterviewSessionsAsync(int userId)
        {
            return await _dbContext.InterviewSessions
                .Include(s => s.InterviewCatalog)
                .Include(s => s.CustomInterview)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId)
        {
            return await _dbContext.InterviewCatalogs
                .Include(c => c.AIAgentRole)
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomInterview>> GetUserCustomInterviewsAsync(int userId)
        {
            return await _dbContext.CustomInterviews
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        private string GenerateInterviewStructure(string type)
        {
            // Generate JSON structure for different interview types
            return type switch
            {
                "career_exploration" => "{\"sections\": [{\"name\": \"Career Goals\", \"questions\": 5}, {\"name\": \"Professional Experience\", \"questions\": 4}, {\"name\": \"Future Plans\", \"questions\": 3}]}",
                "skills_assessment" => "{\"sections\": [{\"name\": \"Technical Skills\", \"questions\": 6}, {\"name\": \"Soft Skills\", \"questions\": 4}, {\"name\": \"Areas for Improvement\", \"questions\": 3}]}",
                "market_positioning" => "{\"sections\": [{\"name\": \"Industry Knowledge\", \"questions\": 4}, {\"name\": \"Personal Brand\", \"questions\": 3}, {\"name\": \"Market Opportunities\", \"questions\": 4}]}",
                "custom" => "{\"sections\": [{\"name\": \"Custom Questions\", \"questions\": 5}]}",
                _ => "{\"sections\": [{\"name\": \"General Questions\", \"questions\": 5}]}"
            };
        }
    }
}
