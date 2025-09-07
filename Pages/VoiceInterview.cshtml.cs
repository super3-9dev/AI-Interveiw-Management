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

namespace InterviewBot.Pages
{
    [Authorize]
    public class VoiceInterviewModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly AppDbContext _dbContext;
        private readonly IInterviewCatalogService _interviewCatalogService;
        private readonly IInterviewCompletionService _interviewCompletionService;
        private readonly IOpenAIService _openAIService;

        public VoiceInterviewModel(IInterviewService interviewService, AppDbContext dbContext, IInterviewCatalogService interviewCatalogService, IInterviewCompletionService interviewCompletionService, IOpenAIService openAIService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _interviewCatalogService = interviewCatalogService;
            _interviewCompletionService = interviewCompletionService;
            _openAIService = openAIService;
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
        public string GreetingMessage { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        private string GetCurrentCulture()
        {
            var currentCulture = HttpContext.Request.Query["culture"].ToString();
            if (string.IsNullOrEmpty(currentCulture))
            {
                currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
            }
            return currentCulture;
        }

        private string GetGreetingMessage()
        {
            var culture = GetCurrentCulture();
            if (culture == "es")
            {
                return "¡Hola! Soy tu coach de carrera con IA. Vamos a tener una entrevista de práctica. Comencemos con la primera pregunta. ¡Di hola para comenzar la entrevista!";
            }
            else
            {
                return "Hello! I am your AI career coach. We are going to have a practice interview. Let us start with the first question. Say hello to start the interview!";
            }
        }

        public async Task<IActionResult> OnGetAsync(string interviewId)
        {
            try
            {
                // Get interviewId from query parameter if not already set
                if (!string.IsNullOrEmpty(interviewId))
                {
                    InterviewId = interviewId;
                }

                Console.WriteLine($"VoiceInterview OnGetAsync - InterviewId: '{InterviewId}'");

                if (string.IsNullOrEmpty(InterviewId))
                {
                    Console.WriteLine("InterviewId is empty, redirecting to Dashboard");
                    ErrorMessage = "Interview ID is required.";
                    return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
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
                    return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
                }

                // Set greeting message based on culture
                GreetingMessage = GetGreetingMessage();

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview: " + ex.Message;
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
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
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
            }
            catch (Exception)
            {
                // If there's an error, still redirect to dashboard
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
            }
        }

        public async Task<IActionResult> OnPostAsync(string handler)
        {
            try
            {
                Console.WriteLine($"VoiceInterview OnPostAsync - Handler====================>: '{handler}'");
                
                // Skip ModelState validation for VoiceChat handler since it uses JSON body
                if (handler != "VoiceChat" && !ModelState.IsValid)
                {
                    await LoadInterviewContentAsync();
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
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
                    case "VoiceChat":
                        return await OnPostVoiceChatAsync();
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
                
                // If no existing messages, add greeting message
                if (messages.Count == 0)
                {
                    InterviewHistory.Add(new InterviewHistoryItem
                    {
                        Question = GetGreetingMessage(),
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
            if (string.IsNullOrEmpty(UserAnswer) && string.IsNullOrEmpty(VoiceRecordingData))
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
                    return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
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
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
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

        public async Task<IActionResult> OnPostVoiceChatAsync()
        {
            try
            {
                Console.WriteLine("Voice Chat endpoint called");
                Console.WriteLine($"Request method: {Request.Method}");
                Console.WriteLine($"Content-Type: {Request.ContentType}");
                Console.WriteLine($"Content-Length: {Request.ContentLength}");

                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                Console.WriteLine($"Request body: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    Console.WriteLine("Request body is empty");
                    return BadRequest(new { error = "Request body is empty" });
                }

                // Try to parse the JSON manually to debug the issue
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                VoiceChatRequest request;
                try
                {
                    request = JsonSerializer.Deserialize<VoiceChatRequest>(requestBody, jsonOptions);
                    Console.WriteLine($"Deserialized request object: {request != null}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                    return BadRequest(new { error = "Invalid JSON format" });
                }

                Console.WriteLine($"Message property value: '{request?.Message}'");
                Console.WriteLine($"Message length: {request?.Message?.Length ?? 0}");
                Console.WriteLine($"InterviewId from request: '{request?.InterviewId}'");

                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    Console.WriteLine("Invalid request - missing message");
                    return BadRequest(new { error = "Invalid request - message is required" });
                }

                // Use interviewId from request if provided, otherwise use the page property
                if (!string.IsNullOrEmpty(request.InterviewId))
                {
                    InterviewId = request.InterviewId;
                    Console.WriteLine($"Using InterviewId from request: '{InterviewId}'");
                }

                // Load interview content to ensure InterviewTopic is set
                await LoadInterviewContentAsync();
                Console.WriteLine($"InterviewTopic after loading: '{InterviewTopic}'");

                // Get interview catalog with agent instructions
                Console.WriteLine($"VoiceInterview - InterviewId===================>: {InterviewId}");
                var interviewCatalog = await _dbContext.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id.ToString() == InterviewId);
                Console.WriteLine($"VoiceInterview - Agent Instructions: {interviewCatalog?.AgentInstructions ?? "Not found"}");

                var userId = GetCurrentUserId();
                Console.WriteLine($"User ID: {userId}");

                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized();
                }

                Console.WriteLine($"InterviewTopic: '{InterviewTopic}'");
                Console.WriteLine($"Generating AI response for topic: {InterviewTopic ?? "Career Interview"}");
                Console.WriteLine($"User message: {request.Message}");
                Console.WriteLine($"Message with instructions: {request.Message}\n\nAgent Instructions: {interviewCatalog?.AgentInstructions ?? ""}");

                // Prepare the message with agent instructions
                var messageWithInstructions = $"{request.Message}\n\nAgent Instructions: {interviewCatalog?.AgentInstructions ?? ""}";
                
                // Ensure we have a valid topic
                var interviewTopic = !string.IsNullOrEmpty(InterviewTopic) ? InterviewTopic : "Career Interview";
                Console.WriteLine($"Final interview topic: '{interviewTopic}'");
                
                Console.WriteLine("Calling OpenAI service...");
                Console.WriteLine($"Message to send to OpenAI: '{messageWithInstructions}'");
                Console.WriteLine($"Interview topic: '{interviewTopic}'");
                Console.WriteLine($"Culture======================>: {HttpContext.Request.Query["culture"].ToString()}");
                
                string aiResponse;
                try
                {
                    // Generate AI response using OpenAI service
                    aiResponse = await _openAIService.GenerateInterviewResponseAsync(
                        messageWithInstructions,
                        interviewTopic,
                        HttpContext.Request.Query["culture"].ToString()
                    );
                    Console.WriteLine("OpenAI service call completed successfully.");
                    Console.WriteLine($"Raw AI response: '{aiResponse}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling OpenAI service: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    aiResponse = "I'm sorry, I'm having trouble processing your request right now. Please try again.";
                }

                Console.WriteLine($"AI Response generated: {aiResponse}");

                // Save chat messages to database
                try
                {
                    Console.WriteLine($"Saving Voice chat messages - UserId: {userId.Value}, InterviewId: '{InterviewId}', UserMessage: '{request.Message}'");
                    
                    // Save the user's original message (without agent instructions)
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, null, request.Message);
                    
                    // Save the AI's response
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, request.Message, aiResponse);
                    
                    Console.WriteLine($"Voice chat messages saved successfully for InterviewId: '{InterviewId}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving chat messages: {ex.Message}");
                }

                return new JsonResult(new { response = aiResponse, isComplete = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Voice chat: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
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

    public class VoiceChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("interviewId")]
        public string InterviewId { get; set; } = string.Empty;
    }
}
