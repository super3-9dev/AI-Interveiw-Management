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
    public class VoiceInterviewModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly AppDbContext _dbContext;

        public VoiceInterviewModel(IInterviewService interviewService, AppDbContext dbContext)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
        }

        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please provide an answer")]
        [StringLength(2000, ErrorMessage = "Answer cannot exceed 2000 characters")]
        public string UserAnswer { get; set; } = string.Empty;

        [BindProperty]
        public string VoiceRecordingData { get; set; } = string.Empty;

        // Interview content properties
        public string InterviewTopic { get; set; } = string.Empty;
        public string InterviewDescription { get; set; } = string.Empty;
        public string CurrentQuestion { get; set; } = string.Empty;
        public List<InterviewHistoryItem> InterviewHistory { get; set; } = new List<InterviewHistoryItem>();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

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

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview: " + ex.Message;
                return RedirectToPage("/Dashboard");
            }
        }

        public async Task<IActionResult> OnPostAsync(string handler)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadInterviewContentAsync();
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return RedirectToPage("/Login");
                }

                switch (handler)
                {
                    case "SubmitAnswer":
                        await SubmitAnswerAsync(userId.Value);
                        break;
                    case "NextQuestion":
                        await MoveToNextQuestionAsync(userId.Value);
                        break;
                    default:
                        ErrorMessage = "Invalid action.";
                        break;
                }

                // Reload interview content
                await LoadInterviewContentAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error processing request: " + ex.Message;
                await LoadInterviewContentAsync();
                return Page();
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
                    InterviewDescription = interviewCatalog.Description;

                    // For now, use the description as the first question
                    CurrentQuestion = interviewCatalog.Description;
                }
            }

            // Load interview history if session exists
            await LoadInterviewHistoryAsync();
        }

        private void LoadDefaultInterviewContent()
        {
            switch (InterviewId)
            {
                case "default-vocational":
                    InterviewTopic = "Vocational Orientation Interview";
                    InterviewDescription = "Explore your interests and values to find a career that aligns with your personality.";
                    CurrentQuestion = "Can you tell me about your main interests and what motivates you in your daily life?";
                    break;
                case "default-professional":
                    InterviewTopic = "Professional Career Interview";
                    InterviewDescription = "Discuss specific roles and industries based on your resume.";
                    CurrentQuestion = "What specific career path are you most interested in pursuing, and why?";
                    break;
                case "default-softskills":
                    InterviewTopic = "Soft Skills Interview";
                    InterviewDescription = "Assess your communication, leadership, and teamwork skills.";
                    CurrentQuestion = "Describe a situation where you had to work with a difficult team member. How did you handle it?";
                    break;
            }
        }

        private async Task LoadInterviewHistoryAsync()
        {
            if (string.IsNullOrEmpty(InterviewId))
                return;

            // In a real implementation, you'd load from InterviewSessions table
            InterviewHistory = new List<InterviewHistoryItem>();
        }

        private async Task SubmitAnswerAsync(int userId)
        {
            if (string.IsNullOrEmpty(UserAnswer) && string.IsNullOrEmpty(VoiceRecordingData))
                return;

            // In a real implementation, you'd save the answer and voice recording to the database
            InterviewHistory.Add(new InterviewHistoryItem
            {
                Question = CurrentQuestion,
                Answer = UserAnswer,
                VoiceRecordingUrl = VoiceRecordingData, // Store voice data as URL for now
                Timestamp = DateTime.Now
            });

            SuccessMessage = "Answer submitted successfully!";
            UserAnswer = string.Empty;
            VoiceRecordingData = string.Empty;

            // Move to next question
            await MoveToNextQuestionAsync(userId);
        }

        private async Task MoveToNextQuestionAsync(int userId)
        {
            // Simple logic: 3 questions = interview complete
            if (InterviewHistory.Count >= 3)
            {
                SuccessMessage = "Interview completed! You've answered all questions.";
                CurrentQuestion = "Interview completed. Thank you for your participation!";
            }
            else
            {
                CurrentQuestion = GenerateNextQuestion();
            }
        }

        private string GenerateNextQuestion()
        {
            if (InterviewId.StartsWith("default-"))
            {
                switch (InterviewId)
                {
                    case "default-vocational":
                        return InterviewHistory.Count == 1
                            ? "What values are most important to you when making decisions?"
                            : "How do you see your interests evolving over the next 5 years?";
                    case "default-professional":
                        return InterviewHistory.Count == 1
                            ? "What specific skills do you think are most important for this career?"
                            : "Where do you see yourself in this field in 3-5 years?";
                    case "default-softskills":
                        return InterviewHistory.Count == 1
                            ? "How do you typically handle stress and pressure in work situations?"
                            : "Can you give an example of when you had to adapt to a major change?";
                }
            }

            return "Please elaborate on your previous answer with more specific examples.";
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
