namespace InterviewBot.Services
{
    public interface IAIAgentService
    {
        Task<string> AskQuestionAsync(string message);
        Task<string> TestOpenAIAsync();
    }
}

