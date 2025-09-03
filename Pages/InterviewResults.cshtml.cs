using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using InterviewBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Pages
{
    [Authorize]
    public class InterviewResultsModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly AppDbContext _dbContext;
        private readonly IInterviewCatalogService _interviewCatalogService;

        public InterviewResultsModel(IInterviewService interviewService, AppDbContext dbContext, IInterviewCatalogService interviewCatalogService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _interviewCatalogService = interviewCatalogService;
        }

        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Summary { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int QuestionCount { get; set; } = 0;

        // Interview content properties
        public string InterviewTopic { get; set; } = string.Empty;
        public string InterviewIntroduction { get; set; } = string.Empty;
        public string InterviewSummary { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(InterviewId))
                {
                    ErrorMessage = "Interview ID is required.";
                    return RedirectToPage("/Dashboard");
                }

                // Load interview content from database
                await LoadInterviewContentAsync();

                if (string.IsNullOrEmpty(InterviewTopic))
                {
                    ErrorMessage = "Interview not found or access denied.";
                    return RedirectToPage("/Dashboard");
                }

                // Set the interview summary from query parameter
                if (!string.IsNullOrEmpty(Summary))
                {
                    InterviewSummary = Summary;
                }

                // Update interview status to "Completed" when results page loads
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    Console.WriteLine($"Updating interview catalog {catalogId} status to Completed");
                    var result = await _interviewCatalogService.UpdateInterviewCatalogStatusAsync(catalogId, "Completed");
                    Console.WriteLine($"Status update result: {result}");
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview results: " + ex.Message;
                return RedirectToPage("/Dashboard");
            }
        }

        private async Task LoadInterviewContentAsync()
        {
            if (string.IsNullOrEmpty(InterviewId))
                return;

            // Handle default interviews
            if (InterviewId.StartsWith("default-"))
            {
                LoadDefaultInterviewContent();
                return;
            }

            // Load from database for real interviews
            if (int.TryParse(InterviewId, out int catalogId))
            {
                var interviewCatalog = await _dbContext.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id == catalogId);

                if (interviewCatalog != null)
                {
                    InterviewTopic = interviewCatalog.Topic;
                    InterviewIntroduction = interviewCatalog.Introduction;
                }
            }
        }

        private void LoadDefaultInterviewContent()
        {
            switch (InterviewId)
            {
                case "default-vocational":
                    InterviewTopic = "Vocational Orientation Interview";
                    InterviewIntroduction = "Explore your interests and values to find a career that aligns with your personality.";
                    break;
                case "default-professional":
                    InterviewTopic = "Professional Career Interview";
                    InterviewIntroduction = "Discuss specific roles and industries based on your resume.";
                    break;
                case "default-softskills":
                    InterviewTopic = "Soft Skills Interview";
                    InterviewIntroduction = "Assess your communication, leadership, and teamwork skills.";
                    break;
            }
        }
    }
}