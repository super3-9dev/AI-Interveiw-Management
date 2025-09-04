using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Services
{
    public class InterviewCatalogService : IInterviewCatalogService
    {
        private readonly AppDbContext _context;

        public InterviewCatalogService(AppDbContext context)
        {
            _context = context;
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
            return await _context.InterviewCatalogs
                .Where(ic => ic.UserId == userId && ic.IsActive)
                .ToListAsync();
        }

        public async Task<List<CustomInterview>> GetUserCustomInterviewsAsync(int userId)
        {
            return await _context.CustomInterviews
                .Where(ci => ci.UserId == userId && ci.IsActive)
                .ToListAsync();
        }

        public async Task<List<InterviewCatalogItem>> GetUserInterviewCatalogItemsAsync(int userId)
        {
            return await _context.InterviewCatalogItems
                .Where(ici => ici.UserId == userId && ici.IsActive)
                .OrderByDescending(ici => ici.CreatedAt)
                .ToListAsync();
        }

        public async Task<InterviewSession> StartInterviewAsync(int catalogId, int userId, InterviewType type)
        {
            var catalog = await _context.InterviewCatalogs
                .FirstOrDefaultAsync(ic => ic.Id == catalogId && ic.UserId == userId);

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
            var session = await _context.InterviewSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = InterviewStatus.Paused;
            session.PausedAt = DateTime.UtcNow;
            session.PauseReason = reason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResumeInterviewAsync(int sessionId, string notes)
        {
            var session = await _context.InterviewSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = InterviewStatus.InProgress;
            session.ResumedAt = DateTime.UtcNow;
            session.ResumeNotes = notes;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteInterviewAsync(int sessionId)
        {
            var session = await _context.InterviewSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = InterviewStatus.Completed;
            session.EndTime = DateTime.UtcNow;
            session.IsCompleted = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InterviewResult> GenerateInterviewAnalysisAsync(int sessionId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.Messages)
                .Include(s => s.AIAgentRole)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new InvalidOperationException("Interview session not found");

            // Generate AI analysis based on the interview
            var analysis = await GenerateAIAnalysis(session);

            // Save the result
            _context.InterviewResults.Add(analysis);
            await _context.SaveChangesAsync();

            return analysis;
        }



        private Task<InterviewResult> GenerateAIAnalysis(InterviewSession session)
        {
            // This would integrate with OpenAI API to generate analysis
            // For now, creating a mock analysis
            var result = new InterviewResult
            {
                UserId = session.UserId ?? 0,
                Topic = "Mock Interview",
                Question = "Mock interview question",
                CompleteDate = DateTime.UtcNow,
                Content = GenerateMockEvaluation(session)
            };

            return Task.FromResult(result);
        }

        private int CalculateMockScore(InterviewSession session)
        {
            // Mock scoring logic
            var messageCount = session.Messages.Count;
            var duration = session.Duration?.TotalMinutes ?? 0;

            var baseScore = Math.Min(100, messageCount * 10);
            var timeBonus = Math.Min(20, (int)(duration * 2));

            return Math.Min(100, baseScore + timeBonus);
        }

        private string GenerateMockEvaluation(InterviewSession session)
        {
            var score = CalculateMockScore(session);

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
