using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IStudentReportService
    {
        Task<StudentReportResponse?> GetStudentReportAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, bool includeAllData = false);
    }
}
