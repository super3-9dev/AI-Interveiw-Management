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

        public NewAnalysisModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        public void OnGet()
        {
            // Initialize the page
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
                            BriefIntroduction,
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
