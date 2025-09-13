using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ReportModel : PageModel
    {
        private readonly ILogger<ReportModel> _logger;
        private readonly IStudentReportService _studentReportService;

        public ReportModel(ILogger<ReportModel> logger, IStudentReportService studentReportService)
        {
            _logger = logger;
            _studentReportService = studentReportService;
        }

        public StudentReportResponse? StudentReport { get; set; }
        public bool IsLoading { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IncludeAllData { get; set; } = false;

        public async Task OnGetAsync(DateTime? fromDate = null, DateTime? toDate = null, bool includeAll = false)
        {
            FromDate = fromDate;
            ToDate = toDate;
            IncludeAllData = includeAll;
            await LoadStudentReport();
        }

        public async Task<IActionResult> OnPostTestApiAsync()
        {
            await LoadStudentReport();
            return Page();
        }

        private async Task LoadStudentReport()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Get the current user ID from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "9"; // Default to "9" for testing (matching the Postman example)
                
                _logger.LogInformation("Loading student report for userId: {UserId}, FromDate: {FromDate}, ToDate: {ToDate}, IncludeAll: {IncludeAll}", 
                    userId, FromDate, ToDate, IncludeAllData);
                
                StudentReport = await _studentReportService.GetStudentReportAsync(userId, FromDate, ToDate, IncludeAllData);
                
                if (StudentReport?.Response != null)
                {
                    _logger.LogInformation("Successfully loaded student report for {Name}", 
                        StudentReport.Response.ClientInfo.Name);
                }
                else
                {
                    _logger.LogInformation("No report data found for userId: {UserId}", userId);
                }
                
                if (StudentReport == null)
                {
                    ErrorMessage = "Unable to load report data. Please try again later.";
                    _logger.LogWarning("StudentReport is null for userId: {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student report");
                ErrorMessage = "An error occurred while loading the report. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
