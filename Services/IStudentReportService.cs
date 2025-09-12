using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IStudentReportService
    {
        Task<StudentReportResponse?> GetStudentReportAsync(string userId);
        Task<ComprehensiveReportResponse?> GetComprehensiveReportAsync(string userId);
    }
}
