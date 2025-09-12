using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Authorization;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ReportModel : PageModel
    {
        private readonly ILogger<ReportModel> _logger;

        public ReportModel(ILogger<ReportModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Initialize any data needed for the report page
        }
    }
}
