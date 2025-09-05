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
        private readonly IInterviewCatalogService _interviewCatalogService;

        public VoiceInterviewModel(IInterviewService interviewService, AppDbContext dbContext, IInterviewCatalogService interviewCatalogService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _interviewCatalogService = interviewCatalogService;
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
        public string InterviewIntroduction { get; set; } = string.Empty;
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

                // Set status to "InProgress" when user starts the interview
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    var userId = GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        // Update catalog status to InProgress
                        await _interviewCatalogService.StartInterviewCatalogAsync(catalogId, userId.Value);
                        Console.WriteLine($"Updated catalog {catalogId} status to InProgress");
                    }
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

        public async Task<IActionResult> OnGetBackToDashboardAsync()
        {
            try
            {
                // Set status to "InProgress" when user goes back to dashboard
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    var userId = GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        await _interviewCatalogService.UpdateInterviewCatalogStatusAsync(catalogId, "InProgress");
                    }
                }
                return RedirectToPage("/Dashboard");
            }
            catch (Exception)
            {
                // If there's an error, still redirect to dashboard
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
                    case "CompleteInterview":
                        return await OnPostCompleteInterviewAsync();
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
                    InterviewIntroduction = interviewCatalog.Introduction;

                    // For now, use the description as the first question
                    CurrentQuestion = interviewCatalog.Introduction;
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
                    InterviewIntroduction = "Explore your interests and values to find a career that aligns with your personality.";
                    CurrentQuestion = "Can you tell me about your main interests and what motivates you in your daily life?";
                    break;
                case "default-professional":
                    InterviewTopic = "Professional Career Interview";
                    InterviewIntroduction = "Discuss specific roles and industries based on your resume.";
                    CurrentQuestion = "What specific career path are you most interested in pursuing, and why?";
                    break;
                case "default-softskills":
                    InterviewTopic = "Soft Skills Interview";
                    InterviewIntroduction = "Assess your communication, leadership, and teamwork skills.";
                    CurrentQuestion = "Describe a situation where you had to work with a difficult team member. How did you handle it?";
                    break;
            }
        }

        private async Task LoadInterviewHistoryAsync()
        {
            if (string.IsNullOrEmpty(InterviewId))
                return;

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return;

                // Load chat messages from database
                var chatMessages = await _interviewService.GetChatMessagesAsync(userId.Value, InterviewId);
                
                // Convert chat messages to interview history
                InterviewHistory = new List<InterviewHistoryItem>();
                var messages = chatMessages.ToList();
                
                for (int i = 0; i < messages.Count; i += 2)
                {
                    if (i + 1 < messages.Count)
                    {
                        InterviewHistory.Add(new InterviewHistoryItem
                        {
                            Question = messages[i].Content, // AI question
                            Answer = messages[i + 1].Content, // User answer
                            Timestamp = messages[i + 1].Timestamp
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading interview history: {ex.Message}");
                InterviewHistory = new List<InterviewHistoryItem>();
            }
        }

        private async Task SubmitAnswerAsync(int userId)
        {
            if (string.IsNullOrEmpty(UserAnswer) && string.IsNullOrEmpty(VoiceRecordingData))
                return;

            // Save the question and answer to chat messages
            try
            {
                Console.WriteLine($"Saving chat messages - UserId: {userId}, InterviewId: '{InterviewId}', Question: '{CurrentQuestion}'");
                
                // Save the AI question
                await _interviewService.SaveChatMessageAsync(userId, InterviewId, null, CurrentQuestion);
                
                // Save the user's answer
                var answerContent = !string.IsNullOrEmpty(UserAnswer) ? UserAnswer : "Voice recording provided";
                await _interviewService.SaveChatMessageAsync(userId, InterviewId, CurrentQuestion, answerContent);
                
                Console.WriteLine($"Chat messages saved successfully for InterviewId: '{InterviewId}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chat messages: {ex.Message}");
            }

            // Add to local history for display
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

        private Task MoveToNextQuestionAsync(int userId)
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
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostCompleteInterviewAsync()
        {
            try
            {
                Console.WriteLine($"OnPostCompleteInterviewAsync called for InterviewId: {InterviewId}");

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    Console.WriteLine("User not authenticated, redirecting to login");
                    return RedirectToPage("/Account/Login");
                }

                Console.WriteLine($"User ID: {userId}");

                // Set status to "Completed" when interview is completed
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    Console.WriteLine($"Updating catalog {catalogId} status to Completed");
                    var result = await _interviewCatalogService.UpdateInterviewCatalogStatusAsync(catalogId, "Completed");
                    Console.WriteLine($"Status update result: {result}");
                }
                else
                {
                    Console.WriteLine($"Failed to parse InterviewId: {InterviewId}");
                }

                // Generate interview summary
                var summary = await GenerateInterviewSummaryAsync();
                Console.WriteLine($"Generated summary: {summary?.Substring(0, Math.Min(100, summary?.Length ?? 0))}...");

                // Redirect to dashboard page
                Console.WriteLine("Redirecting to Dashboard page");
                return RedirectToPage("/Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPostCompleteInterviewAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ErrorMessage = "Error completing interview: " + ex.Message;
                return Page();
            }
        }

        private Task<string> GenerateInterviewSummaryAsync()
        {
            try
            {
                // Generate a simple summary based on interview history
                var summary = $"Interview completed successfully!\n\n";
                summary += $"Total Questions Answered: {InterviewHistory.Count}\n";
                summary += $"Interview Topic: {InterviewTopic}\n\n";
                summary += "Thank you for participating in this interview. Your responses have been recorded and will be used for analysis.";

                return Task.FromResult(summary);
            }
            catch (Exception)
            {
                return Task.FromResult("Interview completed. Summary generation encountered an error, but your responses have been recorded.");
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
