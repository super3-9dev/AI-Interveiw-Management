using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InterviewBot.Services
{
    public class InterviewCatalogService : IInterviewCatalogService
    {
        private readonly AppDbContext _context;
        private readonly IInterviewAnalysisService _analysisService;
        private readonly IProfileService _profileService;
        private readonly ILogger<InterviewCatalogService> _logger;

        public InterviewCatalogService(AppDbContext context, IInterviewAnalysisService analysisService, IProfileService profileService, ILogger<InterviewCatalogService> logger)
        {
            _context = context;
            _analysisService = analysisService;
            _profileService = profileService;
            _logger = logger;
        }

        public async Task<List<InterviewCatalog>> GenerateInterviewCatalogsAsync(int userId, int profileId, Dictionary<string, object>? apiResponseData = null)
        {
            var user = await _context.Users
                .Include(u => u.SelectedAIAgentRole)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.SelectedAIAgentRole == null)
                throw new InvalidOperationException("User or AI agent role not found");

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(r => r.Id == profileId && r.UserId == userId);

            if (profile == null)
                throw new InvalidOperationException("Profile not found");

            var catalogs = new List<InterviewCatalog>();

            // If API response data is provided, use it to create catalogs
            if (apiResponseData != null && apiResponseData.ContainsKey("parsedCatalogs"))
            {
                var parsedCatalogs = apiResponseData["parsedCatalogs"] as object[];
                if (parsedCatalogs != null)
                {
                    foreach (var catalogItem in parsedCatalogs)
                    {
                        if (catalogItem is Dictionary<string, object> catalogData)
                        {
                            var catalog = new InterviewCatalog
                            {
                                UserId = userId,
                                Topic = catalogData.GetValueOrDefault("topic")?.ToString() ?? "Interview Topic",
                                Introduction = catalogData.GetValueOrDefault("instruction")?.ToString() ?? "AI-generated interview catalog based on your profile analysis",
                                AgentInstructions = catalogData.GetValueOrDefault("agentInstructions")?.ToString() ?? "",
                                InterviewType = "AI Generated"
                            };
                            catalogs.Add(catalog);
                        }
                    }
                }
            }

            // If no API data or fallback, generate default catalogs
            if (catalogs.Count == 0)
            {
                // Generate Career Counselling Interview Catalog
                if (user.SelectedAIAgentRole.Name.Contains("Career Counselling"))
                {
                    var careerCatalog = new InterviewCatalog
                    {
                        UserId = userId,
                        Topic = "Career Counselling Interview",
                        Introduction = "Professional career guidance interview based on your resume analysis",
                        InterviewType = "Career Counselling"
                    };
                    catalogs.Add(careerCatalog);
                }

                // Generate Purpose Discovery Interview Catalog
                if (user.SelectedAIAgentRole.Name.Contains("Purpose Discovery"))
                {
                    var purposeCatalog = new InterviewCatalog
                    {
                        UserId = userId,
                        Topic = "Purpose Discovery Interview",
                        Introduction = "Deep exploration of your values, motivations, and life direction",
                        InterviewType = "Purpose Discovery"
                    };
                    catalogs.Add(purposeCatalog);
                }

                // Generate Skills Assessment Interview Catalog
                var skillsCatalog = new InterviewCatalog
                {
                    UserId = userId,
                    Topic = "Skills Assessment Interview",
                    Introduction = "Comprehensive evaluation of your technical and soft skills",
                    InterviewType = "Skills Assessment"
                };
                catalogs.Add(skillsCatalog);
            }

            // Save catalogs to database
            _context.InterviewCatalogs.AddRange(catalogs);
            await _context.SaveChangesAsync();

            return catalogs;
        }

        public async Task<CustomInterview> CreateCustomInterviewAsync(CustomInterview customInterview)
        {
            _context.CustomInterviews.Add(customInterview);
            await _context.SaveChangesAsync();
            return customInterview;
        }

        public async Task<List<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId)
        {
            try
            {
                return await _context.InterviewCatalogs
                    .Where(ic => ic.UserId == userId && ic.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview catalogs for user {UserId}. Table may not exist.", userId);
                return new List<InterviewCatalog>(); // Return empty list if table doesn't exist
            }
        }

        public async Task<List<CustomInterview>> GetUserCustomInterviewsAsync(int userId)
        {
            try
            {
                return await _context.CustomInterviews
                    .Where(ci => ci.UserId == userId && ci.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom interviews for user {UserId}. Table may not exist.", userId);
                return new List<CustomInterview>(); // Return empty list if table doesn't exist
            }
        }

        public async Task<List<InterviewCatalogItem>> GetUserInterviewCatalogItemsAsync(int userId)
        {
            try
            {
                return await _context.InterviewCatalogItems
                    .Where(ici => ici.UserId == userId && ici.IsActive)
                    .OrderByDescending(ici => ici.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview catalog items for user {UserId}. Table may not exist.", userId);
                return new List<InterviewCatalogItem>(); // Return empty list if table doesn't exist
            }
        }

        public async Task<InterviewSession> StartInterviewAsync(int catalogId, int userId, InterviewType type)
        {
            InterviewCatalog? catalog;
            try
            {
                catalog = await _context.InterviewCatalogs
                    .FirstOrDefaultAsync(ic => ic.Id == catalogId && ic.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview catalog {CatalogId} for user {UserId}. Table may not exist.", catalogId, userId);
                throw new InvalidOperationException("Database error: InterviewCatalogs table may not exist");
            }

            if (catalog == null)
                throw new InvalidOperationException("Interview catalog not found");

            var session = new InterviewSession
            {
                UserId = userId,
                Type = type,
                Status = InterviewStatus.InProgress,
                InterviewCatalogId = catalogId,
                StartTime = DateTime.UtcNow
            };

            _context.InterviewSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<bool> PauseInterviewAsync(int sessionId, string reason)
        {
            try
            {
                var session = await _context.InterviewSessions.FindAsync(sessionId);
                if (session == null) return false;

                session.Status = InterviewStatus.Paused;
                session.PausedAt = DateTime.UtcNow;
                session.PauseReason = reason;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing interview session {SessionId}. Table may not exist.", sessionId);
                return false;
            }
        }

        public async Task<bool> ResumeInterviewAsync(int sessionId, string notes)
        {
            try
            {
                var session = await _context.InterviewSessions.FindAsync(sessionId);
                if (session == null) return false;

                session.Status = InterviewStatus.InProgress;
                session.ResumedAt = DateTime.UtcNow;
                session.ResumeNotes = notes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming interview session {SessionId}. Table may not exist.", sessionId);
                return false;
            }
        }

        public async Task<bool> CompleteInterviewAsync(int sessionId)
        {
            try
            {
                var session = await _context.InterviewSessions.FindAsync(sessionId);
                if (session == null) return false;

                session.Status = InterviewStatus.Completed;
                session.EndTime = DateTime.UtcNow;
                session.IsCompleted = true;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing interview session {SessionId}. Table may not exist.", sessionId);
                return false;
            }
        }

        public async Task<bool> CompleteInterviewWithAnalysisAsync(int sessionId)
        {
            try
            {
                _logger.LogInformation("Starting interview completion with analysis for session {SessionId}", sessionId);

                // Get the interview session with related data
                InterviewSession? session;
                try
                {
                    session = await _context.InterviewSessions
                        .Include(s => s.InterviewCatalog)
                        .Include(s => s.CustomInterview)
                        .FirstOrDefaultAsync(s => s.Id == sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving interview session {SessionId}. Table may not exist.", sessionId);
                    return false; // Return false if table doesn't exist
                }

                if (session == null)
                {
                    _logger.LogWarning("Interview session {SessionId} not found", sessionId);
                    return false;
                }

                if (session.UserId == null)
                {
                    _logger.LogWarning("User ID is null for session {SessionId}", sessionId);
                    return false;
                }

                // Get user profile for analysis
                var profile = await _context.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == session.UserId);

                if (profile == null)
                {
                    _logger.LogWarning("Profile not found for user {UserId}", session.UserId);
                    return false;
                }

                // Get chat messages for the interview
                var chatMessages = await _context.ChatMessages
                    .Where(m => m.InterviewId == sessionId.ToString())
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                if (!chatMessages.Any())
                {
                    _logger.LogWarning("No chat messages found for session {SessionId}", sessionId);
                    return false;
                }

                // Build conversation array
                var conversation = new List<InterviewConversation>();
                for (int i = 0; i < chatMessages.Count; i += 2)
                {
                    if (i + 1 < chatMessages.Count)
                    {
                        conversation.Add(new InterviewConversation
                        {
                            Question = chatMessages[i].Content,
                            Answer = chatMessages[i + 1].Content
                        });
                    }
                }

                // Create analysis request
                var analysisRequest = new InterviewAnalysisRequest
                {
                    Purpose = session.InterviewCatalog?.InterviewType ?? "Career Counselling",
                    ResponseLanguage = "en",
                    InterviewName = session.InterviewCatalog?.Topic ?? "Interview",
                    InterviewObjective = $"Assess {session.InterviewCatalog?.InterviewType?.ToLower() ?? "career"} potential and skills",
                    UserProfileBrief = profile.BriefIntroduction ?? "Professional with relevant experience",
                    UserProfileStrength = profile.Strengths ?? "Strong analytical and communication skills",
                    UserProfileWeakness = profile.Weaknesses ?? "Areas for improvement in specific skills",
                    UserProfileFutureCareerGoal = profile.FutureCareerGoals ?? "Advance career in chosen field",
                    UserProfileMotivation = profile.Motivations ?? "Driven by professional growth and impact",
                    InterviewConversation = conversation
                };

                // Call analysis API
                var (success, analysisResponse, errorMessage) = await _analysisService.CallInterviewAnalysisAPIAsync(analysisRequest);

                if (success && analysisResponse != null)
                {
                    // Save analysis result
                    await _analysisService.SaveInterviewAnalysisResultAsync(sessionId, session.UserId.Value, analysisResponse);
                    _logger.LogInformation("Successfully saved interview analysis for session {SessionId}", sessionId);
                }
                else
                {
                    _logger.LogError("Failed to get interview analysis for session {SessionId}: {Error}", sessionId, errorMessage);
                }

                // Complete the interview
                session.Status = InterviewStatus.Completed;
                session.EndTime = DateTime.UtcNow;
                session.IsCompleted = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully completed interview with analysis for session {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing interview with analysis for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<InterviewResult> GenerateInterviewAnalysisAsync(int sessionId)
        {
            InterviewSession? session;
            try
            {
                session = await _context.InterviewSessions
                    .Include(s => s.AIAgentRole)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview session {SessionId}. Table may not exist.", sessionId);
                throw new InvalidOperationException("Database error: InterviewSessions table may not exist");
            }

            if (session == null)
                throw new InvalidOperationException("Interview session not found");

            // Generate AI analysis based on the interview
            var analysis = await GenerateAIAnalysis(session);

            // Save the result
            _context.InterviewResults.Add(analysis);
            await _context.SaveChangesAsync();

            return analysis;
        }



        private async Task<InterviewResult> GenerateAIAnalysis(InterviewSession session)
        {
            // This would integrate with OpenAI API to generate analysis
            // For now, creating a mock analysis
            var result = new InterviewResult
            {
                UserId = session.UserId ?? 0,
                Topic = "Mock Interview",
                Question = "Mock interview question",
                CompleteDate = DateTime.UtcNow,
                Content = await GenerateMockEvaluation(session)
            };

            return result;
        }

        private async Task<int> CalculateMockScore(InterviewSession session)
        {
            // Mock scoring logic
            var messageCount = await _context.ChatMessages
                .CountAsync(m => m.InterviewId == session.Id.ToString());
            var duration = session.Duration?.TotalMinutes ?? 0;

            var baseScore = Math.Min(100, messageCount * 10);
            var timeBonus = Math.Min(20, (int)(duration * 2));

            return Math.Min(100, baseScore + timeBonus);
        }

        private async Task<string> GenerateMockEvaluation(InterviewSession session)
        {
            var score = await CalculateMockScore(session);

            if (score >= 90)
                return "Excellent performance! You demonstrated strong communication skills and deep understanding of the topics discussed.";
            else if (score >= 80)
                return "Very good performance! You showed good knowledge and communication abilities with room for improvement.";
            else if (score >= 70)
                return "Good performance! You have solid foundations but could benefit from more practice and preparation.";
            else
                return "Fair performance. Consider reviewing the topics and practicing more to improve your interview skills.";
        }

        private List<InterviewQuestion> GenerateMockQuestions(InterviewSession session)
        {
            // Generate mock questions and answers based on the session
            var questions = new List<InterviewQuestion>();

            // This would be populated with actual questions from the interview
            // For now, creating placeholder questions

            return questions;
        }

        public async Task<bool> UpdateInterviewCatalogStatusAsync(int catalogId, string status)
        {
            var catalog = await _context.InterviewCatalogs.FindAsync(catalogId);
            if (catalog == null) return false;

            catalog.Status = status;
            catalog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StartInterviewCatalogAsync(int catalogId, int userId)
        {
            var catalog = await _context.InterviewCatalogs
                .FirstOrDefaultAsync(ic => ic.Id == catalogId && ic.UserId == userId);

            if (catalog == null) return false;

            catalog.Status = "InProgress";
            catalog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateInterviewKindAsync(int catalogId, string interviewKind)
        {
            try
            {
                var catalog = await _context.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id == catalogId);

                if (catalog == null)
                    return false;

                catalog.InterviewKind = interviewKind;
                catalog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<InterviewCatalog?> GetInterviewCatalogByIdAsync(int catalogId)
        {
            try
            {
                return await _context.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id == catalogId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
