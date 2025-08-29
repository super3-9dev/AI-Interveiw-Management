using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IInterviewCatalogService
    {
        Task<List<InterviewCatalog>> GenerateInterviewCatalogsAsync(int userId, int profileId);
        Task<CustomInterview> CreateCustomInterviewAsync(CustomInterview customInterview);
        Task<List<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId);
        Task<List<CustomInterview>> GetUserCustomInterviewsAsync(int userId);
        Task<InterviewSession> StartInterviewAsync(int catalogId, int userId, InterviewType type);
        Task<bool> PauseInterviewAsync(int sessionId, string reason);
        Task<bool> ResumeInterviewAsync(int sessionId, string notes);
        Task<bool> CompleteInterviewAsync(int sessionId);
        Task<InterviewResult> GenerateInterviewAnalysisAsync(int sessionId);
    }
}
