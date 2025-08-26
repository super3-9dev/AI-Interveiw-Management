using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ResumeAnalysisResultsModel : PageModel
    {
        private readonly IResumeAnalysisService _resumeAnalysisService;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ResumeAnalysis? ResumeAnalysis { get; private set; }

        public ResumeAnalysisResultsModel(IResumeAnalysisService resumeAnalysisService)
        {
            _resumeAnalysisService = resumeAnalysisService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                ResumeAnalysis = await _resumeAnalysisService.GetResumeAnalysisAsync(Id, userId.Value);
                
                if (ResumeAnalysis == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Log the error
                return StatusCode(500, "An error occurred while retrieving the analysis results.");
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
