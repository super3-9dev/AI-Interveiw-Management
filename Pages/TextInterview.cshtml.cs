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
using System.Text;
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
        private readonly IInterviewAnalysisService _interviewAnalysisService;

        public TextInterviewModel(IInterviewService interviewService, AppDbContext dbContext, IOpenAIService openAIService, IInterviewCatalogService interviewCatalogService, IInterviewCompletionService interviewCompletionService, IInterviewAnalysisService interviewAnalysisService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _openAIService = openAIService;
            _interviewCatalogService = interviewCatalogService;
            _interviewCompletionService = interviewCompletionService;
            _interviewAnalysisService = interviewAnalysisService;
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
        public string AgentInstructions { get; set; } = string.Empty;
        public string CurrentQuestion { get; set; } = string.Empty;
        public List<InterviewHistoryItem> InterviewHistory { get; set; } = new List<InterviewHistoryItem>();
        public string GreetingMessage { get; set; } = string.Empty;

        // Interview completion tracking
        public int QuestionCount { get; set; } = 0;
        public bool IsInterviewComplete { get; set; } = false;
        public string InterviewSummary { get; set; } = string.Empty;

        // Session-based question tracking
        private const string QuestionCountKey = "InterviewQuestionCount";

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
                return "Hello! I'm your AI career coach. We're going to have a practice interview. Let's start with the first question. Say hello to start the interview!";
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

                Console.WriteLine($"TextInterview OnGetAsync - InterviewId: '{InterviewId}'");

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

                // Load question count from session (reset if starting new interview)
                var currentCount = GetQuestionCount();
                if (currentCount == 0)
                {
                    // This is a new interview, ensure count starts at 0
                    SetQuestionCount(0);
                }
                QuestionCount = currentCount;

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
                if (!ModelState.IsValid)
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
                    AgentInstructions = interviewCatalog.AgentInstructions;
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
                                Question = messages[i + 1].Content, // AI question
                                Answer = messages[i].Content, // User answer
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

                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    Console.WriteLine("Invalid request - missing message");
                    return BadRequest(new { error = "Invalid request - message is required" });
                }

                var interviewCatalog = await _dbContext.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id.ToString() == request.InterviewId);

                // Use interviewId from request if provided, otherwise use the page property
                if (!string.IsNullOrEmpty(request.InterviewId))
                {
                    InterviewId = request.InterviewId;
                    Console.WriteLine($"Using InterviewId from request: '{InterviewId}'");
                }

                var userId = GetCurrentUserId();

                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized();
                }

                // Get current question count from session and increment
                var currentCount = GetQuestionCount();
                currentCount++;
                SetQuestionCount(currentCount);

                // Check if interview should end (limit to 6 questions)
                if (currentCount >= 6)
                {
                    var summaryResponse = await GenerateInterviewSummaryAsync();
                    IsInterviewComplete = true;
                    InterviewSummary = summaryResponse;

                    // Clear the question count for next interview
                    ClearQuestionCount();

                    // Call analysis API and store result
                    try
                    {
                        await CallAnalysisApiAndStoreResultAsync(InterviewId, GetCurrentCulture());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error calling analysis API: {ex.Message}");
                        // Continue with interview completion even if API call fails
                    }
                    if (GetCurrentCulture() == "es")
                    {
                        return new JsonResult(new
                        {
                            response = "Gracias por tus respuestas. Ya tengo suficiente información para proporcionarte un análisis completo. Deja que generemos tu resumen de la entrevista...",
                            isComplete = true,
                            summary = summaryResponse,
                            questionCount = currentCount
                        });
                    }
                    else
                    {
                        return new JsonResult(new
                        {
                            response = "Thank you for your responses. I have enough information to provide you with a comprehensive analysis. Let me generate your interview summary...",
                            isComplete = true,
                            summary = summaryResponse,
                            questionCount = currentCount
                        });
                    }
                }

                // Build conversation context for AI
                var conversationContext = BuildConversationContext(request.Message);
                // Generate AI response using OpenAI service
                // Pass the actual user message, not the conversation context
                var aiResponse = await _openAIService.GenerateInterviewResponseAsync(
                    conversationContext,
                    interviewCatalog?.AgentInstructions ?? "Career Interview",
                    HttpContext.Request.Query["culture"].ToString(),
                    interviewCatalog?.InterviewType ?? "text"
                );

                if (aiResponse.Contains("Thank you"))
                {
                    // Call analysis API and store result
                    try
                    {
                        await CallAnalysisApiAndStoreResultAsync(InterviewId, GetCurrentCulture());
                        ClearQuestionCount();
                        return new JsonResult(new
                        {
                            response = aiResponse,
                            isComplete = true,
                            summary = aiResponse,
                            questionCount = currentCount
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error calling analysis API: {ex.Message}");
                        // Continue with interview completion even if API call fails
                    }
                }

                // Check if AI wants to terminate the interview
                bool isTerminated = false;
                string finalResponse = aiResponse;

                if (aiResponse.Contains("INTERVIEW_TERMINATED:"))
                {
                    isTerminated = true;
                    // Extract the termination message
                    var parts = aiResponse.Split("INTERVIEW_TERMINATED:", 2);
                    if (parts.Length > 1)
                    {
                        finalResponse = parts[1].Trim();
                    }

                    // Mark interview as complete
                    SetQuestionCount(1000); // Set high number to indicate termination
                }

                // Save chat messages to database
                try
                {
                    // Save the user's message
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, null, request.Message);

                    // Save the AI's response (use finalResponse which may be cleaned up)
                    await _interviewService.SaveChatMessageAsync(userId.Value, InterviewId, request.Message, finalResponse);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving chat messages: {ex.Message}");
                }

                // Check if AI response indicates interview completion (only after 5+ questions)
                if (currentCount >= 5 && (aiResponse.Contains("interview complete") || aiResponse.Contains("enough information") ||
                    aiResponse.Contains("Thank you")))
                {
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

                return new JsonResult(new
                {
                    response = finalResponse,
                    isComplete = false,
                    isTerminated = isTerminated
                });
            }
            catch (Exception ex)
            {
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

        private string BuildConversationContext(string currentMessage)
        {
            var context = new StringBuilder();

            // Add conversation history
            if (InterviewHistory != null && InterviewHistory.Count > 0)
            {
                context.AppendLine("CONVERSATION HISTORY:");
                context.AppendLine("===================");

                foreach (var item in InterviewHistory)
                {
                    if (!string.IsNullOrEmpty(item.Question))
                    {
                        context.AppendLine($"AI: {item.Question}");
                    }
                    if (!string.IsNullOrEmpty(item.Answer))
                    {
                        context.AppendLine($"User: {item.Answer}");
                    }
                }
                context.AppendLine();
            }

            // Add current user message
            context.AppendLine("CURRENT USER MESSAGE:");
            context.AppendLine("====================");
            context.AppendLine(currentMessage);
            context.AppendLine();

            // Add instructions for AI
            var currentCulture = GetCurrentCulture();
            if (currentCulture == "es")
            {
                context.AppendLine("INSTRUCCIONES:");
                context.AppendLine("==============");
                context.AppendLine("Basándote en el historial de conversación anterior, haz una pregunta NUEVA que no hayas hecho antes.");
                context.AppendLine("NO repitas ninguna pregunta que ya esté en el historial de conversación.");
                context.AppendLine("Haz una pregunta de seguimiento diferente y relevante basada en la respuesta del usuario.");
                context.AppendLine("IMPORTANTE: Responde ÚNICAMENTE en español. NUNCA mezcles inglés con español.");
                context.AppendLine("CRÍTICO: Si el usuario dice 'Hola', 'Hello', 'Sí', 'No', o respuestas cortas similares,");
                context.AppendLine("NO termines la entrevista. En su lugar, haz una pregunta de seguimiento amigable.");
                context.AppendLine("NUNCA uses 'INTERVIEW_TERMINATED:' para respuestas cortas o saludos.");
                context.AppendLine("Solo termina la entrevista si el usuario da respuestas completamente sin sentido después de múltiples intentos.");
                context.AppendLine("TERMINA INMEDIATAMENTE si has logrado el objetivo específico de la entrevista (ej: si necesitas la fecha de nacimiento y el usuario la proporciona).");
                context.AppendLine("USA 'INTERVIEW_TERMINATED:' SOLO cuando hayas completado exitosamente el objetivo de la entrevista.");
                context.AppendLine("EJEMPLO: Si el usuario dice 'Hola', responde: '¡Hola! Me da mucho gusto conocerte. Comencemos con la entrevista. Cuéntame, ¿cuál es tu experiencia profesional más relevante?'");
            }
            else
            {
                context.AppendLine("INSTRUCTIONS:");
                context.AppendLine("=============");
                context.AppendLine("Based on the conversation history above, ask a NEW question that you haven't asked before.");
                context.AppendLine("Do NOT repeat any questions that are already in the conversation history.");
                context.AppendLine("Ask a different, relevant follow-up question based on the user's response.");
                context.AppendLine("IMPORTANT: Respond ONLY in English. Do NOT mix languages.");
                context.AppendLine("CRITICAL: If the user says 'Hello', 'Hi', 'Yes', 'No', or similar short responses,");
                context.AppendLine("Do NOT terminate the interview. Instead, ask a friendly follow-up question.");
                context.AppendLine("NEVER use 'INTERVIEW_TERMINATED:' for short responses or greetings.");
                context.AppendLine("Only terminate the interview if the user gives completely nonsensical responses after multiple attempts.");
                context.AppendLine("TERMINATE IMMEDIATELY if you have achieved the specific interview objective (e.g., if you need the user's birthday and they provide it).");
                context.AppendLine("USE 'INTERVIEW_TERMINATED:' ONLY when you have successfully completed the interview objective.");
                context.AppendLine("EXAMPLE: If the user says 'Hello', respond: 'Hello! Nice to meet you. Let's start the interview. Tell me, what's your most relevant professional experience?'");
            }

            return context.ToString();
        }

        private async Task<InterviewResult> CallAnalysisApiAndStoreResultAsync(string interviewId, string culture)
        {
            try
            {
                // Get interview catalog and user profile
                var interviewCatalog = await _dbContext.InterviewCatalogs
                    .FirstOrDefaultAsync(c => c.Id.ToString() == interviewId);
                if (interviewCatalog != null)
                {
                    interviewCatalog.Status = "Completed";
                    await _dbContext.SaveChangesAsync();
                }
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);
                var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

                if (interviewCatalog == null || user == null)
                {
                    throw new Exception("Interview catalog or user not found");
                }

                // Build conversation history
                var conversation = new List<InterviewConversation>();
                var chatMessages = await _dbContext.ChatMessages
                    .Where(m => m.InterviewId == interviewId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
                for (int i = 0; i < chatMessages.Count; i += 2)
                {
                    if (i + 1 < chatMessages.Count)
                    {
                        conversation.Add(new InterviewConversation
                        {
                            Question = chatMessages[i + 1].Content,
                            Answer = chatMessages[i].Content
                        });
                    }
                }

                // Create API request
                var apiRequest = new InterviewAnalysisRequest
                {
                    Purpose = "Career Counselling",
                    ResponseLanguage = culture == "es" ? "es" : "en",
                    InterviewName = interviewCatalog.Topic,
                    InterviewObjective = interviewCatalog.Introduction ?? "Assess candidate's skills and experience",
                    UserProfileBrief = profile?.BriefIntroduction ?? "Candidate profile information",
                    UserProfileStrength = profile?.Strengths ?? "Candidate strengths",
                    UserProfileWeakness = profile?.Weaknesses ?? "Areas for improvement",
                    UserProfileFutureCareerGoal = profile?.FutureCareerGoals ?? "Career aspirations",
                    UserProfileMotivation = profile?.Motivations ?? "Career motivation",
                    InterviewConversation = conversation,
                    InterviewId = interviewId
                };

                // Call the analysis API
                var (success, apiResponse, errorMessage) = await _interviewAnalysisService.CallInterviewAnalysisAPIAsync(apiRequest);
                // Create or update InterviewResult
                var interviewResult = await _dbContext.InterviewResults
                    .FirstOrDefaultAsync(r => r.InterviewId == interviewId);

                if (interviewResult == null)
                {
                    interviewResult = new InterviewResult
                    {
                        UserId = userId ?? 0,
                        InterviewId = interviewId,
                        Topic = interviewCatalog.Topic,
                        Question = "Interview Analysis",
                        Content = "Interview completed and analyzed",
                        CompleteDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.InterviewResults.Add(interviewResult);
                }

                // Store API response
                interviewResult.ApiRequestPayload = JsonSerializer.Serialize(apiRequest, new JsonSerializerOptions { WriteIndented = true });
                interviewResult.ApiResponse = apiResponse != null ? JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { WriteIndented = true }) : null;
                interviewResult.ApiCallDate = DateTime.UtcNow;
                interviewResult.ApiCallSuccessful = success;
                interviewResult.ApiErrorMessage = errorMessage;
                interviewResult.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return interviewResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling analysis API: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GenerateInterviewSummaryAsync()
        {
            try
            {

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
                    "Interview Summary Generation",
                    GetCurrentCulture()
                );

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
                Console.WriteLine($"OnPostCompleteInterviewAsync called for InterviewId: {HttpContext.Request.Query["interviewId"]}");

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
                try
                {
                    await CallAnalysisApiAndStoreResultAsync(InterviewId, GetCurrentCulture());
                    ClearQuestionCount();
                    Console.WriteLine("Analysis API called successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling analysis API: {ex.Message}");
                    // Continue with interview completion even if API call fails
                }

                // Also call the completion service for additional processing
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

                // Redirect to InterviewResults page
                Console.WriteLine("Redirecting to InterviewResults page");
                return RedirectToPage("/InterviewResults", new { interviewId = InterviewId, culture = GetCurrentCulture() });
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

        [JsonPropertyName("interviewId")]
        public string InterviewId { get; set; } = string.Empty;
    }
}
