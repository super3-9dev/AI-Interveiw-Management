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
        private readonly IProfileService _profileService;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public Profile? Profile { get; private set; }

        public ResumeAnalysisResultsModel(IProfileService profileService)
        {
            _profileService = profileService;
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

                Profile = await _profileService.GetProfileAsync(Id, userId.Value);

                if (Profile == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception)
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
                    var success = await _profileService.RetryAnalysisAsync(analysisId, userId.Value);

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
            catch (Exception)
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

                var profile = await _profileService.GetProfileAsync(Id, userId.Value);
                if (profile == null)
                {
                    return NotFound();
                }

                return new JsonResult(new
                {
                    status = profile.Status,
                    progress = profile.Progress
                });
            }
            catch (Exception)
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
