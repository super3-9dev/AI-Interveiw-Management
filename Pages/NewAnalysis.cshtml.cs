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
        private readonly IResumeAnalysisService _resumeAnalysisService;

        [BindProperty]
        public IFormFile? ResumeFile { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public NewAnalysisModel(IResumeAnalysisService resumeAnalysisService)
        {
            _resumeAnalysisService = resumeAnalysisService;
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
                    ErrorMessage = "User not authenticated.";
                    return Page();
                }

                // Validate file if uploaded
                if (ResumeFile != null)
                {
                    // Check file size (10MB limit)
                    if (ResumeFile.Length > 10 * 1024 * 1024)
                    {
                        ErrorMessage = "File size must be less than 10MB.";
                        return Page();
                    }

                    // Check file type - only PDF for now
                    var allowedExtensions = new[] { ".pdf" };
                    var fileExtension = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ErrorMessage = "Only PDF files are allowed.";
                        return Page();
                    }

                    // Upload and analyze resume
                    try
                    {
                        var resumeAnalysis = await _resumeAnalysisService.UploadAndAnalyzeResumeAsync(ResumeFile, userId.Value);
                        
                        SuccessMessage = $"Resume '{ResumeFile.FileName}' uploaded successfully! AI analysis is in progress.";
                        
                        // Redirect to a results page or dashboard
                        return RedirectToPage("/ResumeAnalysisResults", new { id = resumeAnalysis.Id });
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Error uploading resume: {ex.Message}";
                        return Page();
                    }
                }

                // Validate that at least one input method is provided
                if (ResumeFile == null && string.IsNullOrWhiteSpace(Description))
                {
                    ErrorMessage = "Please either upload a resume or provide a description of yourself.";
                    return Page();
                }

                // Handle text-based analysis (if needed in the future)
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    // TODO: Implement text-based analysis
                    SuccessMessage = "Text analysis feature coming soon!";
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
