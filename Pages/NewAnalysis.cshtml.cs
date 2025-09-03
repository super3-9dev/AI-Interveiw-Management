using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class NewAnalysisModel : PageModel
    {
        private readonly IProfileService _profileService;

        [BindProperty]
        public IFormFile? ResumeFile { get; set; }

        [BindProperty]
        public string? BriefIntroduction { get; set; }

        [BindProperty]
        public string? CareerGoals { get; set; }

        [BindProperty]
        public string? CurrentActivity { get; set; }

        [BindProperty]
        public string? Motivations { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        private readonly IExternalAPIService _externalAPIService;
        private readonly IInterviewCatalogService _interviewCatalogService;

        public NewAnalysisModel(IProfileService profileService, IExternalAPIService externalAPIService, IInterviewCatalogService interviewCatalogService)
        {
            _profileService = profileService;
            _externalAPIService = externalAPIService;
            _interviewCatalogService = interviewCatalogService;
        }

        public async Task OnGetAsync()
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                // Get user's latest profile to populate form fields
                var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var latestProfile = userProfiles.FirstOrDefault();

                if (latestProfile != null)
                {
                    // Populate form fields with data from the latest profile
                    BriefIntroduction = latestProfile.BriefIntroduction;
                    CareerGoals = latestProfile.CareerGoals;
                    CurrentActivity = latestProfile.CurrentActivities;
                    Motivations = latestProfile.Motivations;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Get current user ID
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, message = "User not authenticated" });
                    }
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Validate file if uploaded
                if (ResumeFile != null)
                {
                    // Check file size (10MB limit)
                    if (ResumeFile.Length > 10 * 1024 * 1024)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = "File size must be less than 10MB." });
                        }
                        ErrorMessage = "File size must be less than 10MB.";
                        return Page();
                    }

                    // Check file type - only PDF for now
                    var allowedExtensions = new[] { ".pdf" };
                    var fileExtension = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = "Only PDF files are allowed." });
                        }
                        ErrorMessage = "Only PDF files are allowed.";
                        return Page();
                    }

                    // Upload and analyze resume
                    try
                    {
                        var profile = await _profileService.UploadAndAnalyzeResumeAsync(ResumeFile, userId.Value);

                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new
                            {
                                success = true,
                                message = $"Resume '{ResumeFile.FileName}' analyzed successfully!",
                                redirectUrl = $"/ResumeAnalysisResults/{profile.Id}"
                            });
                        }

                        SuccessMessage = $"Resume '{ResumeFile.FileName}' uploaded successfully! AI analysis is in progress.";

                        // Redirect to a results page or dashboard
                        return RedirectToPage("/ResumeAnalysisResults", new { id = profile.Id });
                    }
                    catch (Exception ex)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = $"Error uploading resume: {ex.Message}" });
                        }
                        ErrorMessage = $"Error uploading resume: {ex.Message}";
                        return Page();
                    }
                }

                // Validate that at least one input method is provided
                if (ResumeFile == null && string.IsNullOrWhiteSpace(BriefIntroduction))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, message = "Please either upload a resume or provide a description of yourself." });
                    }
                    ErrorMessage = "Please either upload a resume or provide a description of yourself.";
                    return Page();
                }

                // Handle text-based analysis
                if (!string.IsNullOrWhiteSpace(BriefIntroduction))
                {
                    try
                    {
                        var profile = await _profileService.CreateProfileFromDescriptionAsync(
                            BriefIntroduction ?? "",
                            CareerGoals ?? "",
                            CurrentActivity ?? "",
                            Motivations ?? "",
                            userId.Value);

                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new
                            {
                                success = true,
                                message = "Profile created successfully! AI analysis is in progress.",
                                redirectUrl = $"/ResumeAnalysisResults/{profile.Id}"
                            });
                        }

                        SuccessMessage = "Profile created successfully! AI analysis is in progress.";

                        // Redirect to results page
                        return RedirectToPage("/ResumeAnalysisResults", new { id = profile.Id });
                    }
                    catch (Exception ex)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = $"Error creating profile: {ex.Message}" });
                        }
                        ErrorMessage = $"Error creating profile: {ex.Message}";
                        return Page();
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
                }
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDescriptionAnalysisAsync()
        {
            try
            {
                // Get current user ID
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(BriefIntroduction) || string.IsNullOrWhiteSpace(CurrentActivity))
                {
                    return new JsonResult(new { success = false, message = "Brief Introduction and Current Activities are required" });
                }

                if (string.IsNullOrWhiteSpace(CareerGoals) || string.IsNullOrWhiteSpace(Motivations))
                {
                    return new JsonResult(new { success = false, message = "Please fill in your Career Goals and Motivations" });
                }

                // Prepare API request data
                var interviewCatalogRequest = new
                {
                    briefIntroduction = BriefIntroduction,
                    futureCareerGoals = CareerGoals,
                    currentActivities = CurrentActivity,
                    motivations = Motivations
                };

                // Call the external API
                var apiResponse = await _externalAPIService.SendInterviewCatalogRequestAsync(interviewCatalogRequest);

                if (!apiResponse.Success)
                {
                    return new JsonResult(new { success = false, message = $"API Error: {apiResponse.ErrorMessage}" });
                }

                // Get user's latest profile to get the profile ID
                var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var latestProfile = userProfiles.FirstOrDefault();

                if (latestProfile == null)
                {
                    return new JsonResult(new { success = false, message = "No profile found. Please create a profile first." });
                }

                // Generate interview catalogs from the API response
                var catalogs = await _interviewCatalogService.GenerateInterviewCatalogsAsync(userId.Value, latestProfile.Id, apiResponse.Data);

                return new JsonResult(new
                {
                    success = true,
                    message = "Interview catalogs generated successfully!",
                    catalogCount = catalogs.Count,
                    redirectUrl = "/Dashboard"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
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
