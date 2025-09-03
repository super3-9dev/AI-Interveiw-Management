using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IInterviewCatalogService
    {
        Task<List<InterviewCatalog>> GenerateInterviewCatalogsAsync(int userId, int profileId, Dictionary<string, object>? apiResponseData = null);
        Task<CustomInterview> CreateCustomInterviewAsync(CustomInterview customInterview);
        Task<List<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId);
        Task<List<CustomInterview>> GetUserCustomInterviewsAsync(int userId);
        Task<List<InterviewCatalogItem>> GetUserInterviewCatalogItemsAsync(int userId);
        Task<InterviewSession> StartInterviewAsync(int catalogId, int userId, InterviewType type);
        Task<bool> PauseInterviewAsync(int sessionId, string reason);
        Task<bool> ResumeInterviewAsync(int sessionId, string notes);
        Task<bool> CompleteInterviewAsync(int sessionId);
        Task<InterviewResult> GenerateInterviewAnalysisAsync(int sessionId);
        Task<bool> UpdateInterviewCatalogStatusAsync(int catalogId, string status);
        Task<bool> StartInterviewCatalogAsync(int catalogId, int userId);
    }
}
