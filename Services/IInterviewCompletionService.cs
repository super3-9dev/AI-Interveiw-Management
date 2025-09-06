using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IInterviewCompletionService
    {
        Task<bool> CompleteInterviewWithAnalysisAsync(int userId, string interviewId, string interviewName, string interviewObjective, List<ChatMessage> chatMessages, Profile userProfile);
    }
}
