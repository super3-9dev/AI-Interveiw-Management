using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using InterviewBot.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Pages
{
    [Authorize]
    public class CustomInterviewModel : PageModel
    {
        private readonly IInterviewService _interviewService;

        public CustomInterviewModel(IInterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Interview topic is required")]
        [StringLength(200, ErrorMessage = "Interview topic cannot exceed 200 characters")]
        public string InterviewTopic { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        private string GetCurrentCulture()
        {
            var currentCulture = HttpContext.Request.Query["culture"].ToString();
            if (string.IsNullOrEmpty(currentCulture))
            {
                currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
            }
            return currentCulture;
        }

        public void OnGet()
        {
            // Initialize with default values if needed
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Create the interview catalog entry
                var interviewCatalog = new InterviewCatalog
                {
                    UserId = userId.Value,
                    Topic = InterviewTopic,
                    Introduction = Description,
                    InterviewType = "Custom",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Store directly in InterviewCatalogs table
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                dbContext.InterviewCatalogs.Add(interviewCatalog);
                await dbContext.SaveChangesAsync();

                SuccessMessage = "Custom interview created successfully! You can now find it on your Dashboard.";

                // Clear the form
                InterviewTopic = string.Empty;
                Description = string.Empty;

                // Redirect to dashboard after a short delay to show the success message
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error creating custom interview: " + ex.Message;
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
