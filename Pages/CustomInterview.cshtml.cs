using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Pages
{
    [Authorize]
    public class CustomInterviewModel : PageModel
    {
        private readonly IInterviewCatalogService _interviewCatalogService;

        public CustomInterviewModel(IInterviewCatalogService interviewCatalogService)
        {
            _interviewCatalogService = interviewCatalogService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Interview topic is required")]
        [StringLength(200, ErrorMessage = "Interview topic cannot exceed 200 characters")]
        public string CustomTitle { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string CustomDescription { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Key questions are required")]
        [StringLength(2000, ErrorMessage = "Key questions cannot exceed 2000 characters")]
        public string CustomQuestions { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Focus areas are required")]
        [StringLength(500, ErrorMessage = "Focus areas cannot exceed 500 characters")]
        public string FocusAreas { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Difficulty level is required")]
        public string DifficultyLevel { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Interview duration is required")]
        public string InterviewDuration { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

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

                // Create the custom interview
                var interview = new CustomInterview
                {
                    UserId = userId.Value,
                    Title = CustomTitle,
                    Description = CustomDescription,
                    CustomQuestions = CustomQuestions,
                    FocusAreas = FocusAreas,
                    DifficultyLevel = DifficultyLevel,
                    InterviewDuration = InterviewDuration,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _interviewCatalogService.CreateCustomInterviewAsync(interview);

                SuccessMessage = "Custom interview created successfully! You can now find it on your Dashboard.";

                // Clear the form
                CustomTitle = string.Empty;
                CustomDescription = string.Empty;
                CustomQuestions = string.Empty;
                FocusAreas = string.Empty;
                DifficultyLevel = string.Empty;
                InterviewDuration = string.Empty;

                // Redirect to dashboard after a short delay to show the success message
                return RedirectToPage("/Dashboard");
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
