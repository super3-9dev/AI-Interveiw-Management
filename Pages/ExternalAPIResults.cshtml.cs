using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ExternalAPIResultsModel : PageModel
    {
        private readonly IProfileService _profileService;

        public Profile? Profile { get; set; }
        public string? ErrorMessage { get; set; }

        public ExternalAPIResultsModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                Profile = await _profileService.GetProfileAsync(id, userId.Value);
                if (Profile == null)
                {
                    ErrorMessage = "Profile not found or you don't have permission to view it.";
                    return Page();
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
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
