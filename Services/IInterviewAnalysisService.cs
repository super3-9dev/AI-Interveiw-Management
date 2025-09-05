using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IInterviewAnalysisService
    {
        Task<(bool Success, InterviewAnalysisResponse? Response, string? ErrorMessage)> CallInterviewAnalysisAPIAsync(InterviewAnalysisRequest request);
        Task<InterviewAnalysisResult> SaveInterviewAnalysisResultAsync(int interviewSessionId, int userId, InterviewAnalysisResponse apiResponse);
        Task<InterviewAnalysisResult?> GetInterviewAnalysisResultAsync(int interviewSessionId);
        Task<List<InterviewAnalysisResult>> GetUserInterviewAnalysisResultsAsync(int userId);
    }
}
