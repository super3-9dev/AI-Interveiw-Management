using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using InterviewBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace InterviewBot.Pages
{
    [Authorize]
    public class TextInterviewModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly AppDbContext _dbContext;
        private readonly IOpenAIService _openAIService;
        private readonly IInterviewCatalogService _interviewCatalogService;
        private readonly IInterviewCompletionService _interviewCompletionService;

        public TextInterviewModel(IInterviewService interviewService, AppDbContext dbContext, IOpenAIService openAIService, IInterviewCatalogService interviewCatalogService, IInterviewCompletionService interviewCompletionService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _openAIService = openAIService;
            _interviewCatalogService = interviewCatalogService;
            _interviewCompletionService = interviewCompletionService;
        }

        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please provide an answer")]
        [StringLength(2000, ErrorMessage = "Answer cannot exceed 2000 characters")]
        public string UserAnswer { get; set; } = string.Empty;

        // Interview content properties
        public string InterviewTopic { get; set; } = string.Empty;
        public string InterviewIntroduction { get; set; } = string.Empty;
        public string CurrentQuestion { get; set; } = string.Empty;
        public List<InterviewHistoryItem> InterviewHistory { get; set; } = new List<InterviewHistoryItem>();

        // Interview completion tracking
        public int QuestionCount { get; set; } = 0;
        public bool IsInterviewComplete { get; set; } = false;
        public string InterviewSummary { get; set; } = string.Empty;

        // Session-based question tracking
        private const string QuestionCountKey = "InterviewQuestionCount";

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

                // Load question count from session (reset if starting new interview)
                var currentCount = GetQuestionCount();
                if (currentCount == 0)
                {
                    // This is a new interview, ensure count starts at 0
                    SetQuestionCount(0);
                }
                QuestionCount = currentCount;

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
                    // In a real implementation, you'd have a separate questions table
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
                
                // If no existing messages, add greeting message
                if (messages.Count == 0)
                {
                    InterviewHistory.Add(new InterviewHistoryItem
                    {
                        Question = "Hello! I'm your AI career coach. We're going to have a practice interview. Let's start with the first question. say hello to start the interview!.\n\nSay hello to start the interview!",
                        Answer = "",
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading interview history: {ex.Message}");
                InterviewHistory = new List<InterviewHistoryItem>();
            }
        }

        private async Task SubmitAnswerAsync(int userId)
        {
            if (string.IsNullOrEmpty(UserAnswer))
                return;

            // Check if this is a response to the greeting
            bool isGreetingResponse = InterviewHistory.Count == 1 && 
                                    InterviewHistory[0].Question.Contains("Say hello to start the interview") && 
                                    InterviewHistory[0].Answer == "";

            // Save the question and answer to chat messages
            try
            {
                Console.WriteLine($"Saving chat messages - UserId: {userId}, InterviewId: '{InterviewId}', Question: '{CurrentQuestion}'");
                
                // Save the AI question
                await _interviewService.SaveChatMessageAsync(userId, InterviewId, null, CurrentQuestion);
                
                // Save the user's answer
                await _interviewService.SaveChatMessageAsync(userId, InterviewId, CurrentQuestion, UserAnswer);
                
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
                Timestamp = DateTime.Now
            });

            SuccessMessage = "Answer submitted successfully!";
            UserAnswer = string.Empty;

            // If this was a greeting response, generate the first real interview question
            if (isGreetingResponse)
            {
                CurrentQuestion = GenerateNextQuestion();
            }
            else
            {
                // Move to next question
                await MoveToNextQuestionAsync(userId);
            }
        }

        // API endpoint for OpenAI chat
        public async Task<IActionResult> OnPostOpenAIChatAsync()
        {
            try
            {
                Console.WriteLine("OpenAI Chat endpoint called");

                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                Console.WriteLine($"Request body: {requestBody}");

                // Try to parse the JSON manually to debug the issue
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<OpenAIChatRequest>(requestBody, jsonOptions);
                Console.WriteLine($"Deserialized request object: {request != null}");
                Console.WriteLine($"Message property value: '{request?.Message}'");
                Console.WriteLine($"Message length: {request?.Message?.Length ?? 0}");

                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    Console.WriteLine("Invalid request - missing message");
                    return BadRequest(new { error = "Invalid request - message is required" });
                }

                var userId = GetCurrentUserId();
                Console.WriteLine($"User ID: {userId}");

                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized();
                }

                Console.WriteLine($"Generating AI response for topic: {InterviewTopic ?? "Career Interview"}");
                Console.WriteLine($"User message: {request.Message}");

                // Get current question count from session and increment
                var currentCount = GetQuestionCount();
                currentCount++;
                SetQuestionCount(currentCount);
                Console.WriteLine($"Question count: {currentCount}");

                // Check if interview should end (limit to 10 questions)
                if (currentCount >= 10)
                {
                    Console.WriteLine("Interview ending - reached maximum questions (10)");
                    var summaryResponse = await GenerateInterviewSummaryAsync();
                    IsInterviewComplete = true;
                    InterviewSummary = summaryResponse;

                    // Clear the question count for next interview
                    ClearQuestionCount();

                    return new JsonResult(new
                    {
                        response = "Thank you for your responses. I have enough information to provide you with a comprehensive analysis. Let me generate your interview summary...",
                        isComplete = true,
                        summary = summaryResponse,
                        questionCount = currentCount
                    });
                }

                // Generate AI response using OpenAI service
                var aiResponse = await _openAIService.GenerateInterviewResponseAsync(
                    request.Message,
                    InterviewTopic ?? "Career Interview"
                );

                Console.WriteLine($"AI Response generated: {aiResponse}");

                // Save chat messages to database
                try
                {
                    Console.WriteLine($"Saving OpenAI chat messages - UserId: {userId.Value}, InterviewId: '{InterviewId}', UserMessage: '{request.Message}'");
                    
                    // Save the user's message
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, null, request.Message);
                    
                    // Save the AI's response
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, request.Message, aiResponse);
                    
                    Console.WriteLine($"OpenAI chat messages saved successfully for InterviewId: '{InterviewId}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving chat messages: {ex.Message}");
                }

                // Check if AI response indicates interview completion (only after 5+ questions)
                if (currentCount >= 5 && (aiResponse.Contains("interview complete") || aiResponse.Contains("enough information") ||
                    aiResponse.Contains("thank you")))
                {
                    Console.WriteLine("AI indicates interview should end");
                    var summaryResponse = await GenerateInterviewSummaryAsync();
                    IsInterviewComplete = true;
                    InterviewSummary = summaryResponse;

                    // Clear the question count for next interview
                    ClearQuestionCount();

                    return new JsonResult(new
                    {
                        response = aiResponse,
                        isComplete = true,
                        summary = summaryResponse,
                        questionCount = currentCount
                    });
                }

                return new JsonResult(new { response = aiResponse, isComplete = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OpenAI chat: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        private int GetQuestionCount()
        {
            var count = HttpContext.Session.GetInt32(QuestionCountKey);
            return count ?? 0;
        }

        private void SetQuestionCount(int count)
        {
            HttpContext.Session.SetInt32(QuestionCountKey, count);
            QuestionCount = count;
        }

        private void ClearQuestionCount()
        {
            HttpContext.Session.Remove(QuestionCountKey);
            QuestionCount = 0;
        }

        private async Task<string> GenerateInterviewSummaryAsync()
        {
            try
            {
                Console.WriteLine("Generating interview summary...");

                var summaryPrompt = $@"
                Based on the interview conversation, provide a comprehensive summary and analysis.
                
                Interview Topic: {InterviewTopic}
                Interview Introduction: {InterviewIntroduction}
                Number of Questions Asked: {QuestionCount}
                
                Interview History:
                {string.Join("\n", InterviewHistory.Select((item, index) => $"Q{index + 1}: {item.Question}\nA{index + 1}: {item.Answer}"))}
                
                Please provide:
                1. A brief summary of the key points discussed
                2. Assessment of the candidate's responses
                3. Key strengths identified
                4. Areas for improvement (if any)
                5. Overall recommendation
                
                Format the response in a clear, professional manner suitable for a career interview summary.
                ";

                var summary = await _openAIService.GenerateInterviewResponseAsync(
                    summaryPrompt,
                    "Interview Summary Generation"
                );

                Console.WriteLine($"Interview summary generated successfully");
                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating interview summary: {ex.Message}");
                return "Interview completed. Summary generation encountered an error, but your responses have been recorded.";
            }
        }

        private Task MoveToNextQuestionAsync(int userId)
        {
            // In a real implementation, you'd have a questions table and move through them
            // For now, we'll just show a completion message
            if (InterviewHistory.Count >= 3) // Simple logic: 3 questions = interview complete
            {
                SuccessMessage = "Interview completed! You've answered all questions.";
                CurrentQuestion = "Interview completed. Thank you for your participation!";
            }
            else
            {
                // Generate next question based on interview type
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

                // Get chat messages for analysis
                var chatMessages = await _interviewService.GetChatMessagesAsync(userId.Value, InterviewId);
                Console.WriteLine($"Retrieved {chatMessages.Count()} chat messages for analysis");

                // Get user profile
                var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId.Value);
                if (userProfile == null)
                {
                    Console.WriteLine("User profile not found, cannot complete analysis");
                    ErrorMessage = "User profile not found. Please complete your profile first.";
                    return Page();
                }

                // Get interview catalog details
                var interviewCatalog = await _dbContext.InterviewCatalogs.FirstOrDefaultAsync(ic => ic.Id.ToString() == InterviewId);
                var interviewName = interviewCatalog?.Topic ?? "Interview";
                var interviewObjective = interviewCatalog?.Introduction ?? "General interview assessment";

                // Call analysis API
                Console.WriteLine("Calling analysis API...");
                var analysisSuccess = await _interviewCompletionService.CompleteInterviewWithAnalysisAsync(
                    userId.Value, 
                    InterviewId, 
                    interviewName, 
                    interviewObjective, 
                    chatMessages.ToList(), 
                    userProfile
                );

                if (analysisSuccess)
                {
                    Console.WriteLine("Analysis completed successfully");
                }
                else
                {
                    Console.WriteLine("Analysis failed, but continuing with interview completion");
                }

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

            // For custom interviews, use the description as a base
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

    public class InterviewHistoryItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string VoiceRecordingUrl { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class OpenAIChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
