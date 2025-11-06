using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using InterviewBot.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

        [BindProperty]
        public string Culture { get; set; } = string.Empty;

        public IEnumerable<InterviewCatalog> InterviewCatalogs { get; set; } = new List<InterviewCatalog>();
        public IEnumerable<InterviewSession> ActiveInterviewSessions { get; set; } = new List<InterviewSession>();
        public IEnumerable<CustomInterview> CustomInterviews { get; set; } = new List<CustomInterview>();
        
        // Tasks from groups the user belongs to
        public List<GroupTask> Tasks { get; set; } = new List<GroupTask>();
        public bool IsStudent { get; set; } = false;
        public bool IsInAnyGroup { get; set; } = false;

        public Profile? UserProfile { get; set; }
        public bool HasProfiles { get; set; } = false;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        private readonly AppDbContext _db;

        public DashboardModel(IInterviewService interviewService, IProfileService profileService, IInterviewCatalogService interviewCatalogService, AppDbContext db)
        {
            _interviewService = interviewService;
            _profileService = profileService;
            _interviewCatalogService = interviewCatalogService;
            _db = db;
        }

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await LoadDataAsync(userId.Value);
            }
            
            // Check if user is a student and load tasks if applicable
            await CheckStudentAndLoadTasksAsync();
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

        public async Task<IActionResult> OnPostContinueInterviewAsync(int interviewId, string culture)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Get the interview catalog to check the interviewKind
                var catalog = await _interviewCatalogService.GetInterviewCatalogByIdAsync(interviewId);
                if (catalog == null)
                {
                    ErrorMessage = "Interview not found.";
                    return Page();
                }

                // Get current culture
                var currentCulture = !string.IsNullOrEmpty(Culture) ? Culture : HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }

                // Redirect based on interviewKind
                if (catalog.InterviewKind?.ToLower() == "voice")
                {
                    return RedirectToPage("/VoiceInterview", new { interviewId = interviewId, culture = currentCulture });
                }
                else
                {
                    // Default to text interview if interviewKind is "text" or empty/null
                    return RedirectToPage("/TextInterview", new { interviewId = interviewId, culture = currentCulture });
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Error continuing interview.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteInterviewCatalogAsync(int catalogId)
        {
            try
            {
                Console.WriteLine($"DeleteInterviewCatalogAsync called with catalogId: {catalogId}");
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return RedirectToPage("/Account/Login");
                }

                Console.WriteLine($"Deleting catalog {catalogId} for user {userId.Value}");
                var success = await _interviewService.DeleteInterviewCatalogAsync(catalogId, userId.Value);
                if (success)
                {
                    Console.WriteLine("Interview deleted successfully");
                    SuccessMessage = "Interview deleted successfully.";
                }
                else
                {
                    Console.WriteLine("Failed to delete interview");
                    ErrorMessage = "Failed to delete interview. Please try again.";
                }

                // Reload data
                await LoadDataAsync(userId.Value);
                
                // Preserve culture parameter in redirect
                var culture = Request.Form["culture"].FirstOrDefault();
                if (!string.IsNullOrEmpty(culture))
                {
                    return RedirectToPage("/Dashboard", new { culture = culture });
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting interview: {ex.Message}");
                ErrorMessage = "Error deleting interview: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteCustomInterviewAsync(int customInterviewId)
        {
            try
            {
                Console.WriteLine($"DeleteCustomInterviewAsync called with customInterviewId: {customInterviewId}");
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return RedirectToPage("/Account/Login");
                }

                Console.WriteLine($"Deleting custom interview {customInterviewId} for user {userId.Value}");
                var success = await _interviewService.DeleteCustomInterviewAsync(customInterviewId, userId.Value);
                if (success)
                {
                    Console.WriteLine("Custom interview deleted successfully");
                    SuccessMessage = "Custom interview deleted successfully.";
                }
                else
                {
                    Console.WriteLine("Failed to delete custom interview");
                    ErrorMessage = "Failed to delete custom interview. Please try again.";
                }

                // Reload data
                await LoadDataAsync(userId.Value);
                
                // Preserve culture parameter in redirect
                var culture = Request.Form["culture"].FirstOrDefault();
                if (!string.IsNullOrEmpty(culture))
                {
                    return RedirectToPage("/Dashboard", new { culture = culture });
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting custom interview: {ex.Message}");
                ErrorMessage = "Error deleting custom interview: " + ex.Message;
                return Page();
            }
        }

        private async Task LoadDataAsync(int userId)
        {
            try
            {
                // Check if user has any profiles
                var userProfiles = await _profileService.GetUserProfilesAsync(userId);
                HasProfiles = userProfiles.Any();

                // Load user profile for display
                if (HasProfiles)
                {
                    UserProfile = userProfiles.FirstOrDefault();
                }

                // Only load interview data if user has profiles
                if (HasProfiles)
                {
                    InterviewCatalogs = await _interviewService.GetUserInterviewCatalogsAsync(userId);
                    ActiveInterviewSessions = await _interviewService.GetUserInterviewSessionsAsync(userId);
                    CustomInterviews = await _interviewService.GetUserCustomInterviewsAsync(userId);

                    // If no interview catalogs exist, generate some default ones
                    if (!InterviewCatalogs.Any())
                    {
                        var userProfile = userProfiles.FirstOrDefault();
                        if (userProfile != null)
                        {
                            // Generate default interview catalogs
                            var defaultCatalogs = await _interviewCatalogService.GenerateInterviewCatalogsAsync(userId, userProfile.Id);
                            InterviewCatalogs = defaultCatalogs;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview data: " + ex.Message;
            }
        }

        private async Task CheckStudentAndLoadTasksAsync()
        {
            // Check if user is a student (IdProfile == 2)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 2)
            {
                IsStudent = false;
                IsInAnyGroup = false;
                Tasks = new List<GroupTask>();
                return;
            }

            IsStudent = true;

            // Get current user's email
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                IsInAnyGroup = false;
                Tasks = new List<GroupTask>();
                return;
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                IsInAnyGroup = false;
                Tasks = new List<GroupTask>();
                return;
            }

            // Find groups where the user's email is included in the Emails field (comma-separated)
            var allGroups = await _db.Groups
                .Where(g => !string.IsNullOrWhiteSpace(g.Emails))
                .ToListAsync();

            var userGroups = allGroups
                .Where(g => 
                {
                    if (string.IsNullOrWhiteSpace(g.Emails))
                        return false;

                    // Split emails by comma and check for exact match (case-insensitive)
                    var emails = g.Emails
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim().ToLowerInvariant())
                        .ToList();

                    return emails.Contains(user.Email.Trim().ToLowerInvariant());
                })
                .ToList();

            if (userGroups == null || !userGroups.Any())
            {
                IsInAnyGroup = false;
                Tasks = new List<GroupTask>();
                return;
            }

            IsInAnyGroup = true;

            // Get all tasks from groups the user belongs to (only visible tasks)
            var groupIds = userGroups.Select(g => g.Id).ToList();
            Tasks = await _db.Tasks
                .Where(t => groupIds.Contains(t.GroupId) && t.IsVisible == true)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
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
