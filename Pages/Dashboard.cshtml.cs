using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly IProfileService _profileService;
        private readonly IInterviewCatalogService _interviewCatalogService;

        [BindProperty]
        public string CustomTitle { get; set; } = string.Empty;

        [BindProperty]
        public string CustomDescription { get; set; } = string.Empty;

        [BindProperty]
        public string CustomQuestions { get; set; } = string.Empty;

        [BindProperty]
        public string FocusAreas { get; set; } = string.Empty;

        [BindProperty]
        public string DifficultyLevel { get; set; } = string.Empty;

        [BindProperty]
        public string InterviewDuration { get; set; } = string.Empty;

        public IEnumerable<InterviewCatalog> InterviewCatalogs { get; set; } = new List<InterviewCatalog>();
        public IEnumerable<InterviewSession> ActiveInterviewSessions { get; set; } = new List<InterviewSession>();
        public IEnumerable<CustomInterview> CustomInterviews { get; set; } = new List<CustomInterview>();

        public Profile? UserProfile { get; set; }
        public bool HasProfiles { get; set; } = false;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public DashboardModel(IInterviewService interviewService, IProfileService profileService, IInterviewCatalogService interviewCatalogService)
        {
            _interviewService = interviewService;
            _profileService = profileService;
            _interviewCatalogService = interviewCatalogService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    // Check if user has any profiles
                    var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                    HasProfiles = userProfiles.Any();

                    // Load user profile for display
                    if (HasProfiles)
                    {
                        UserProfile = userProfiles.FirstOrDefault();
                    }

                    // Only load interview data if user has profiles
                    if (HasProfiles)
                    {
                        InterviewCatalogs = await _interviewService.GetUserInterviewCatalogsAsync(userId.Value);
                        ActiveInterviewSessions = await _interviewService.GetUserInterviewSessionsAsync(userId.Value);
                        CustomInterviews = await _interviewService.GetUserCustomInterviewsAsync(userId.Value);

                        // If no interview catalogs exist, generate some default ones
                        if (!InterviewCatalogs.Any())
                        {
                            var userProfile = userProfiles.FirstOrDefault();
                            if (userProfile != null)
                            {
                                // Generate default interview catalogs
                                var defaultCatalogs = await _interviewCatalogService.GenerateInterviewCatalogsAsync(userId.Value, userProfile.Id);
                                InterviewCatalogs = defaultCatalogs;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview data: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Create custom interview
                var catalog = await _interviewService.CreateCustomInterviewAsync(
                    CustomTitle,
                    CustomDescription,
                    CustomQuestions,
                    FocusAreas,
                    DifficultyLevel,
                    InterviewDuration,
                    userId.Value);

                SuccessMessage = "Custom interview created successfully!";

                // Reload data
                InterviewCatalogs = await _interviewService.GetUserInterviewCatalogsAsync(userId.Value);
                ActiveInterviewSessions = await _interviewService.GetUserInterviewSessionsAsync(userId.Value);
                CustomInterviews = await _interviewService.GetUserCustomInterviewsAsync(userId.Value);

                return Page();
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
