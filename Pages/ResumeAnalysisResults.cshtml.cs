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

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                var action = Request.Form["action"].ToString();
                
                if (action == "retry")
                {
                    var analysisId = int.Parse(Request.Form["analysisId"].ToString());
                    var success = await _resumeAnalysisService.RetryAnalysisAsync(analysisId, userId.Value);
                    
                    if (success)
                    {
                        // Redirect back to the same page to show the updated status
                        return RedirectToPage("/ResumeAnalysisResults", new { id = analysisId });
                    }
                    else
                    {
                        return NotFound();
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        public async Task<IActionResult> OnGetStatusAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                var analysis = await _resumeAnalysisService.GetResumeAnalysisAsync(Id, userId.Value);
                if (analysis == null)
                {
                    return NotFound();
                }

                return new JsonResult(new
                {
                    status = analysis.Status,
                    progress = analysis.Progress
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while checking status.");
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
