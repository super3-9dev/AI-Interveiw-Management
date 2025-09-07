using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class InterviewFormatModel : PageModel
    {
        private readonly IInterviewCatalogService _interviewCatalogService;

        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        [BindProperty]
        public string Culture { get; set; } = string.Empty;

        public InterviewFormatModel(IInterviewCatalogService interviewCatalogService)
        {
            _interviewCatalogService = interviewCatalogService;
        }

        public void OnGet()
        {
            // The InterviewId is automatically bound from the query string
        }

        public async Task<IActionResult> OnPostStartTextInterviewAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                if (int.TryParse(InterviewId, out int catalogId))
                {
                    // Update the interviewKind to "text" for text interview
                    await _interviewCatalogService.UpdateInterviewKindAsync(catalogId, "text");
                }
                // Redirect to text interview page
                var currentCulture = !string.IsNullOrEmpty(Culture) ? Culture : 
                    (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");

                return RedirectToPage("/TextInterview", new { interviewId = InterviewId, culture = currentCulture });
            }
            catch (Exception)
            {
                // Log error and redirect to dashboard
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }
                return RedirectToPage("/Dashboard", new { culture = currentCulture });
            }
        }

        public async Task<IActionResult> OnPostStartVoiceInterviewAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                if (int.TryParse(InterviewId, out int catalogId))
                {
                    // Update the interviewKind to "voice" for voice interview
                    await _interviewCatalogService.UpdateInterviewKindAsync(catalogId, "voice");
                }

                // Redirect to voice interview page
                var currentCulture = !string.IsNullOrEmpty(Culture) ? Culture : 
                    (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");

                return RedirectToPage("/VoiceInterview", new { interviewId = InterviewId, culture = currentCulture });
            }
            catch (Exception)
            {
                // Log error and redirect to dashboard
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }
                return RedirectToPage("/Dashboard", new { culture = currentCulture });
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
