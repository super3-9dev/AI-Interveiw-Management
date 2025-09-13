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

namespace InterviewBot.Pages
{
    [Authorize]
    public class InterviewResultsModel : PageModel
    {
        private readonly IInterviewService _interviewService;
        private readonly AppDbContext _dbContext;
        private readonly IInterviewCatalogService _interviewCatalogService;
        private readonly IInterviewAnalysisService _analysisService;

        public InterviewResultsModel(IInterviewService interviewService, AppDbContext dbContext, IInterviewCatalogService interviewCatalogService, IInterviewAnalysisService analysisService)
        {
            _interviewService = interviewService;
            _dbContext = dbContext;
            _interviewCatalogService = interviewCatalogService;
            _analysisService = analysisService;
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
        public DateTime? CompleteDate { get; set; }

        // Analysis results properties
        public InterviewAnalysisResult? AnalysisResult { get; set; }
        public string Recommendations { get; set; } = string.Empty;
        public string MBAFocusArea { get; set; } = string.Empty;
        public int ClarityScore { get; set; }
        public List<CareerRoadmapItem> YourCareerRoadmaps { get; set; } = new();
        public List<string> AdditionalTips { get; set; } = new();

        public string? ErrorMessage { get; set; }

        private string GetCurrentCulture()
        {
            var currentCulture = HttpContext.Request.Query["culture"].ToString();
            if (string.IsNullOrEmpty(currentCulture))
            {
                currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
            }
            return currentCulture;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(InterviewId))
                {
                    ErrorMessage = "Interview ID is required.";
                    return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated.";
                    return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
                }

                // Try to load analysis results first
                bool hasAnalysisResults = await LoadAnalysisResultsAsync(userId.Value);

                if (!hasAnalysisResults)
                {
                    // If no analysis results, try to load stored interview results
                bool hasStoredResults = await LoadStoredInterviewResultAsync(userId.Value);

                if (!hasStoredResults)
                {
                    // If no stored results, load from catalog and save new results
                    await LoadInterviewContentAsync();

                    if (string.IsNullOrEmpty(InterviewTopic))
                    {
                        ErrorMessage = "Interview not found or access denied.";
                            return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
                    }

                    // Set the interview summary from query parameter
                    if (!string.IsNullOrEmpty(Summary))
                    {
                        InterviewSummary = Summary;
                    }
                        else
                        {
                            // Generate default summary for voice interviews
                            InterviewSummary = GenerateContent();
                        }

                        // Set question count for voice interviews
                        QuestionCount = 10;

                    // Update interview status to "Completed" when results page loads
                    if (int.TryParse(InterviewId, out int catalogId))
                    {
                        Console.WriteLine($"Updating interview catalog {catalogId} status to Completed");
                        var result = await _interviewCatalogService.UpdateInterviewCatalogStatusAsync(catalogId, "Completed");
                        Console.WriteLine($"Status update result: {result}");

                        // Save interview results to database
                        await SaveInterviewResultsAsync(catalogId);
                        }
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading interview results: " + ex.Message;
                return RedirectToPage("/Dashboard", new { culture = GetCurrentCulture() });
            }
        }

        private async Task<bool> LoadAnalysisResultsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"Loading analysis results for user {userId} and InterviewId {InterviewId}");

                if (int.TryParse(InterviewId, out int sessionId))
                {
                    // Try to find analysis result by session ID
                    AnalysisResult = await _analysisService.GetInterviewAnalysisResultAsync(sessionId);

                    if (AnalysisResult != null)
                    {
                        Console.WriteLine($"Found analysis result: SessionId={AnalysisResult.InterviewSessionId}, Summary={AnalysisResult.Summary?.Substring(0, Math.Min(50, AnalysisResult.Summary?.Length ?? 0))}...");

                        // Load analysis data
                        Summary = AnalysisResult.Summary ?? "No summary available";
                        Recommendations = AnalysisResult.Recommendations ?? "No recommendations available";
                        MBAFocusArea = AnalysisResult.MBAFocusArea ?? "Not specified";
                        ClarityScore = AnalysisResult.ClarityScore;

                        // Parse roadmaps from JSON
                        if (!string.IsNullOrEmpty(AnalysisResult.YourCareerRoadmaps))
                        {
                            YourCareerRoadmaps = JsonSerializer.Deserialize<List<CareerRoadmapItem>>(AnalysisResult.YourCareerRoadmaps) ?? new List<CareerRoadmapItem>();
                        }
                        if (!string.IsNullOrEmpty(AnalysisResult.AdditionalTips))
                        {
                            AdditionalTips = JsonSerializer.Deserialize<List<string>>(AnalysisResult.AdditionalTips) ?? new List<string>();
                        }

                        // Set interview topic from session
                        var session = await _dbContext.InterviewSessions
                            .Include(s => s.InterviewCatalog)
                            .FirstOrDefaultAsync(s => s.Id == sessionId);

                        if (session?.InterviewCatalog != null)
                        {
                            InterviewTopic = session.InterviewCatalog.Topic;
                        }

                        CompleteDate = AnalysisResult.CreatedAt;
                        QuestionCount = 10; // Default for analysis results

                        Console.WriteLine($"Loaded analysis result: {InterviewTopic}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("No analysis result found for this session ID");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid InterviewId format for analysis results");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading analysis results: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                return false;
            }
        }

        private async Task<bool> LoadStoredInterviewResultAsync(int userId)
        {
            try
            {
                Console.WriteLine($"Loading stored interview result for user {userId} and InterviewId {InterviewId}");

                if (int.TryParse(InterviewId, out int catalogId))
                {
                    // Find the stored interview result for this user and interview ID
                    // Using string comparison to handle data type mismatch
                    var storedResult = await _dbContext.InterviewResults
                        .Where(r => r.UserId == userId && r.InterviewId == catalogId.ToString())
                        .OrderByDescending(r => r.CompleteDate)
                        .FirstOrDefaultAsync();

                    if (storedResult != null)
                    {
                        Console.WriteLine($"Found stored result: InterviewId={storedResult.InterviewId}, Topic={storedResult.Topic}, Content={storedResult.Content?.Substring(0, Math.Min(50, storedResult.Content?.Length ?? 0))}...");

                        InterviewTopic = storedResult.Topic;
                        InterviewSummary = storedResult.Content ?? "No content available";
                        QuestionCount = 6; // Voice interviews now have 6 questions
                        CompleteDate = storedResult.CompleteDate;

                        // Check if we have API response data
                        if (!string.IsNullOrEmpty(storedResult.ApiResponse) && storedResult.ApiCallSuccessful)
                        {
                            Console.WriteLine("Found API response data, parsing...");
                            await ParseApiResponseAsync(storedResult.ApiResponse);
                        }

                        Console.WriteLine($"Loaded interview result: {InterviewTopic}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("No stored interview result found for this interview ID");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid InterviewId format");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stored interview result: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                return false;
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

        private async Task SaveInterviewResultsAsync(int catalogId)
        {
            try
            {
                Console.WriteLine($"Saving interview results for catalog {catalogId}");

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    Console.WriteLine("User not authenticated, cannot save results");
                    return;
                }

                // Check if results already exist for this user and interview
                var existingResult = await _dbContext.InterviewResults
                    .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.InterviewId == catalogId.ToString());

                if (existingResult != null)
                {
                    Console.WriteLine($"Interview results already exist for user {userId.Value} and interview: {catalogId}");
                    return;
                }

                // Create new interview result with simplified structure
                var interviewResult = new InterviewResult
                {
                    UserId = userId.Value,
                    InterviewId = catalogId.ToString(),
                    Topic = InterviewTopic,
                    Question = GenerateQuestion(),
                    CompleteDate = DateTime.UtcNow,
                    Content = !string.IsNullOrEmpty(Summary) ? Summary : GenerateContent(),
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.InterviewResults.Add(interviewResult);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Interview results saved successfully for user {userId.Value} and topic: {InterviewTopic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving interview results: {ex.Message}");
            }
        }

        private string GenerateQuestion()
        {
            return $"Interview completed with {QuestionCount} questions asked about {InterviewTopic}";
        }

        private string GenerateContent()
        {
            return $"1. Summary of Key Points: The interview covered the candidate's experience, skills, achievements, and career goals, highlighting their background in {InterviewTopic} and passion for innovation.\n\n" +
                   $"2. Assessment of Responses: The candidate articulated their experiences effectively, showcasing a blend of technical expertise and leadership qualities. Their passion for driving innovation was evident throughout the conversation.\n\n" +
                   $"3. Key Strengths: The candidate demonstrated strong project management skills, a proactive approach to problem-solving, and a clear enthusiasm for embracing new technologies and methodologies in their field.";
        }

        private Task ParseApiResponseAsync(string apiResponseJson)
        {
            try
            {
                Console.WriteLine($"Parsing API response: {apiResponseJson?.Substring(0, Math.Min(200, apiResponseJson?.Length ?? 0))}...");

                if (string.IsNullOrEmpty(apiResponseJson))
                {
                    Console.WriteLine("API response is empty, skipping parsing");
                    return Task.CompletedTask;
                }

                // Parse the JSON response
                var apiResponse = JsonSerializer.Deserialize<JsonElement>(apiResponseJson);

                // Extract the response data
                if (apiResponse.TryGetProperty("response", out var responseElement))
                {
                    if (responseElement.TryGetProperty("catalog", out var catalogElement))
                    {
                        if (catalogElement.TryGetProperty("InterviewSummary", out var catalogIdElement))
                        {
                            // Parse summary
                            if (catalogIdElement.TryGetProperty("Summary", out var summaryElement))
                            {
                                Summary = summaryElement.GetString() ?? "";
                                Console.WriteLine($"Parsed summary: {Summary?.Substring(0, Math.Min(100, Summary?.Length ?? 0))}...");
                            }

                            // Parse recommendations
                            if (catalogIdElement.TryGetProperty("Recommendations", out var recommendationsElement))
                            {
                                Recommendations = recommendationsElement.GetString() ?? "";
                                Console.WriteLine($"Parsed recommendations: {Recommendations?.Substring(0, Math.Min(100, Recommendations?.Length ?? 0))}...");
                            }
                        }
                        if (catalogElement.TryGetProperty("MBAFocusArea", out var mbaFocusAreaElement))
                        {
                            MBAFocusArea = mbaFocusAreaElement.GetString() ?? "";
                            Console.WriteLine($"Parsed MBA focus area: {MBAFocusArea}");
                        }
                        if (catalogElement.TryGetProperty("YourCareerRoadmaps", out var yourCareerRoadmapsElement))
                        {
                            YourCareerRoadmaps = ParseCareerRoadmaps(yourCareerRoadmapsElement);
                            Console.WriteLine($"Parsed {YourCareerRoadmaps.Count} career roadmaps");
                        }
                        if (catalogElement.TryGetProperty("AdditionalTips", out var additionalTipsElement))
                        {
                            AdditionalTips = ParseAdditionalTips(additionalTipsElement);
                            Console.WriteLine($"Parsed {AdditionalTips.Count} additional tips");
                        }
                    }



                    // Parse MBA focus area
                    if (responseElement.TryGetProperty("mbaFocusArea", out var mbaFocusElement))
                    {
                        MBAFocusArea = mbaFocusElement.GetString() ?? "";
                        Console.WriteLine($"Parsed MBA focus area: {MBAFocusArea}");
                    }

                    // Parse clarity score
                    if (responseElement.TryGetProperty("clarityScore", out var clarityScoreElement))
                    {
                        if (clarityScoreElement.ValueKind == JsonValueKind.Number)
                        {
                            ClarityScore = clarityScoreElement.GetInt32();
                            Console.WriteLine($"Parsed clarity score: {ClarityScore}");
                        }
                    }

                    // Parse career roadmaps
                    if (responseElement.TryGetProperty("yourCareerRoadmaps", out var roadmapsElement))
                    {
                        YourCareerRoadmaps = ParseCareerRoadmaps(roadmapsElement);
                        Console.WriteLine($"Parsed {YourCareerRoadmaps.Count} career roadmaps");
                    }

                    // Parse additional tips
                    if (responseElement.TryGetProperty("additionalTips", out var tipsElement))
                    {
                        AdditionalTips = ParseAdditionalTips(tipsElement);
                        Console.WriteLine($"Parsed {AdditionalTips.Count} additional tips");
                    }
                }
                else
                {
                    Console.WriteLine("No 'response' property found in API response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing API response: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
            }

            return Task.CompletedTask;
        }

        private List<CareerRoadmapItem> ParseCareerRoadmaps(JsonElement roadmapsElement)
        {
            var roadmaps = new List<CareerRoadmapItem>();

            try
            {
                if (roadmapsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var roadmapElement in roadmapsElement.EnumerateArray())
                    {
                        var roadmap = new CareerRoadmapItem();
                        if (roadmapElement.TryGetProperty("title", out var titleElement))
                        {
                            roadmap.Title = titleElement.GetString() ?? "";
                        }

                        if (roadmapElement.TryGetProperty("steps", out var stepsElement))
                        {
                            foreach (var stepElement in stepsElement.EnumerateArray())
                            {
                                roadmap.Steps.Add(stepElement.GetString() ?? "");
                            }
                        }

                        if (!string.IsNullOrEmpty(roadmap.Title) && roadmap.Steps.Any())
                        {
                            roadmaps.Add(roadmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing career roadmaps: {ex.Message}");
            }

            return roadmaps;
        }

        private List<string> ParseAdditionalTips(JsonElement tipsElement)
        {
            var tips = new List<string>();

            try
            {
                if (tipsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tipElement in tipsElement.EnumerateArray())
                    {
                        tips.Add(tipElement.GetString() ?? "");
                    }
                }
                else if (tipsElement.ValueKind == JsonValueKind.String)
                {
                    // If it's a single string, split by common delimiters
                    var tipText = tipsElement.GetString() ?? "";
                    if (!string.IsNullOrEmpty(tipText))
                    {
                        // Split by common delimiters and clean up
                        var splitTips = tipText.Split(new[] { '\n', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tip in splitTips)
                        {
                            var cleanTip = tip.Trim();
                            if (!string.IsNullOrEmpty(cleanTip))
                            {
                                tips.Add(cleanTip);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing additional tips: {ex.Message}");
            }

            return tips;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        public string GetRoadmapIcon(int index)
        {
            var icons = new[]
            {
                "bi bi-link-45deg",
                "bi bi-arrow-up-circle",
                "bi bi-book",
                "bi bi-graph-up",
                "bi bi-trophy",
                "bi bi-star",
                "bi bi-lightbulb",
                "bi bi-people",
                "bi bi-gear",
                "bi bi-briefcase",
                "bi bi-target",
            };

            return icons[index % icons.Length];
        }
    }

    public class CareerRoadmapItem
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
    }
}