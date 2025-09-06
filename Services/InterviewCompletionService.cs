using InterviewBot.Models;
using InterviewBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace InterviewBot.Services
{
    public class InterviewCompletionService : IInterviewCompletionService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<InterviewCompletionService> _logger;

        public InterviewCompletionService(HttpClient httpClient, AppDbContext dbContext, ILogger<InterviewCompletionService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CompleteInterviewWithAnalysisAsync(int userId, string interviewId, string interviewName, string interviewObjective, List<ChatMessage> chatMessages, Profile userProfile)
        {
            try
            {
                _logger.LogInformation("Starting interview completion with analysis for user {UserId}, interview {InterviewId}", userId, interviewId);

                // Build the API request
                var requestData = new
                {
                    purpose = "Career Counselling",
                    responseLanguage = "en",
                    InterviewName = interviewName,
                    InterviewObjective = interviewObjective,
                    userProfileBrief = $"{userProfile.BriefIntroduction}",
                    userProfileStrength = $"{userProfile.Strengths}",
                    userProfileWeakness = $"{userProfile.Weaknesses}",
                    userProfileFutureCareerGoal = $"{userProfile.FutureCareerGoals}",
                    userProfileMotivation = $"{userProfile.Motivations}",
                    interviewConversation = chatMessages
                        .Where(m => !string.IsNullOrEmpty(m.Content))
                        .Select((m, index) => new
                        {
                            question = index % 2 == 0 ? m.Content : chatMessages.ElementAtOrDefault(index - 1)?.Content ?? "",
                            answer = index % 2 == 1 ? m.Content : ""
                        })
                        .Where(c => !string.IsNullOrEmpty(c.question) && !string.IsNullOrEmpty(c.answer))
                        .ToList()
                };

                // Call the analysis API
                var apiResponse = await CallAnalysisAPIAsync(requestData);
                if (apiResponse == null)
                {
                    _logger.LogError("Failed to get analysis from API for interview {InterviewId}", interviewId);
                    return false;
                }

                // Parse and save the response
                await SaveAnalysisResultAsync(userId, interviewId, apiResponse);

                _logger.LogInformation("Successfully completed interview analysis for user {UserId}, interview {InterviewId}", userId, interviewId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing interview with analysis for user {UserId}, interview {InterviewId}", userId, interviewId);
                return false;
            }
        }

        private async Task<string?> CallAnalysisAPIAsync(object requestData)
        {
            try
            {
                var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://plataform.arandutechia.com/webhook/getAnalysisResult", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Analysis API call successful");
                    return responseContent;
                }
                else
                {
                    _logger.LogError("Analysis API call failed with status {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling analysis API");
                return null;
            }
        }

        private async Task SaveAnalysisResultAsync(int userId, string interviewId, string apiResponse)
        {
            try
            {
                var responseData = JsonSerializer.Deserialize<JsonElement>(apiResponse);
                var catalog = responseData.GetProperty("response").GetProperty("catalog");

                var analysisResult = new InterviewAnalysisResult
                {
                    InterviewSessionId = int.Parse(interviewId),
                    UserId = userId,
                    Summary = catalog.TryGetProperty("InterviewSummary", out var summary) ? summary.GetProperty("Summary").GetString() : null,
                    Recommendations = catalog.TryGetProperty("InterviewSummary", out var summary2) ? summary2.GetProperty("Recommendations").GetString() : null,
                    MBAFocusArea = catalog.TryGetProperty("MBAFocusArea", out var mba) ? mba.GetString() : null,
                    ClarityScore = catalog.TryGetProperty("clarityScore", out var score) ? score.GetInt32() : 0,
                    YourCareerRoadmaps = catalog.TryGetProperty("YourCareerRoadmaps", out var roadmaps) ? roadmaps.GetRawText() : null,
                    AdditionalTips = catalog.TryGetProperty("AdditionalTips", out var tips) ? tips.GetRawText() : null,
                    RawApiResponse = apiResponse,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.InterviewAnalysisResults.Add(analysisResult);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Analysis result saved successfully for interview {InterviewId}", interviewId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving analysis result for interview {InterviewId}", interviewId);
                throw;
            }
        }
    }
}
