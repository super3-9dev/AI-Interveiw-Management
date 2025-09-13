using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ReportFilterModel : PageModel
    {
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

        public IActionResult OnPost()
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

                    if (FromDate > DateTime.Now || ToDate > DateTime.Now)
                    {
                        ErrorMessage = "Dates cannot be in the future.";
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
            catch (Exception)
            {
                ErrorMessage = "An error occurred while processing your request. Please try again.";
                return Page();
            }
        }
    }
}
