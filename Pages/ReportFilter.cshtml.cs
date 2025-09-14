using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ReportFilterModel : PageModel
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ReportFilterModel> _logger;

        public ReportFilterModel(AppDbContext dbContext, ILogger<ReportFilterModel> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [BindProperty]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [BindProperty]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [BindProperty]
        [Display(Name = "Include All Data")]
        public bool IncludeAllData { get; set; } = false;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Set default date range (last 30 days)
            if (!FromDate.HasValue)
            {
                FromDate = DateTime.Now.AddDays(-30);
            }
            if (!ToDate.HasValue)
            {
                ToDate = DateTime.Now;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!IncludeAllData)
                {
                    if (!FromDate.HasValue || !ToDate.HasValue)
                    {
                        ErrorMessage = "Please select both from and to dates.";
                        return Page();
                    }

                    if (FromDate > ToDate)
                    {
                        ErrorMessage = "From date cannot be later than to date.";
                        return Page();
                    }

                    // if (FromDate > DateTime.Now || ToDate > DateTime.Now)
                    // {
                    //     ErrorMessage = "Dates cannot be in the future.";
                    //     return Page();
                    // }

                    // Check if there are interviews within the selected date range
                    var hasInterviews = await CheckForInterviewsInDateRange();
                    if (!hasInterviews)
                    {
                        ErrorMessage = "No interviews found within the selected date range. Please select a different date range or check 'Include all available data' to view all interviews.";
                        return Page();
                    }
                }

                // Redirect to Report page with date parameters
                var culture = Request.Query["culture"].ToString();
                
                if (IncludeAllData)
                {
                    return RedirectToPage("/Report", new { culture = culture, includeAll = true });
                }
                else
                {
                    return RedirectToPage("/Report", new { 
                        culture = culture, 
                        fromDate = FromDate?.ToString("yyyy-MM-dd"), 
                        toDate = ToDate?.ToString("yyyy-MM-dd") 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing report filter request");
                ErrorMessage = "An error occurred while processing your request. Please try again.";
                return Page();
            }
        }

        private async Task<bool> CheckForInterviewsInDateRange()
        {
            try
            {
                // Get the current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in claims");
                    return false;
                }

                // Convert dates to UTC for database comparison
                // Use start of day for fromDate and end of day for toDate to include the full day
                var fromDateUtc = FromDate?.Date.ToUniversalTime(); // Start of day
                var toDateUtc = ToDate?.Date.AddDays(1).AddTicks(-1).ToUniversalTime(); // End of day

                _logger.LogInformation("Original dates - FromDate: {FromDate}, ToDate: {ToDate}", FromDate, ToDate);
                _logger.LogInformation("UTC dates - FromDate: {FromDateUtc}, ToDate: {ToDateUtc}", fromDateUtc, toDateUtc);
                _logger.LogInformation("Checking for interviews between {FromDate} and {ToDate} for user {UserId}", 
                    fromDateUtc, toDateUtc, userId);

                // Check if there are any InterviewResults within the date range
                var hasInterviews = await _dbContext.InterviewResults
                    .Where(ir => ir.UserId == userId)
                    .Where(ir => !string.IsNullOrEmpty(ir.InterviewId))
                    .Where(ir => ir.CompleteDate >= fromDateUtc && 
                                 ir.CompleteDate <= toDateUtc)
                    .AnyAsync();

                _logger.LogInformation("Found interviews in date range: {HasInterviews}", hasInterviews);
                return hasInterviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for interviews in date range");
                return false;
            }
        }
    }
}
