using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace InterviewBot.Services
{
    public class InterviewAnalysisService : IInterviewAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<InterviewAnalysisService> _logger;

        public InterviewAnalysisService(HttpClient httpClient, AppDbContext dbContext, ILogger<InterviewAnalysisService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<(bool Success, InterviewAnalysisResponse? Response, string? ErrorMessage)> CallInterviewAnalysisAPIAsync(InterviewAnalysisRequest request)
        {
            try
            {
                _logger.LogInformation("Calling interview analysis API for interview: {InterviewName}", request.InterviewName);

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to API endpoint: https://plataform.arandutechia.com/webhook/getAnalysisResult");
                _logger.LogInformation("Request content: {Content}", json);

                var response = await _httpClient.PostAsync("https://plataform.arandutechia.com/webhook/getAnalysisResult", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Interview analysis API response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Interview analysis API response content length: {Length}", responseContent.Length);
                _logger.LogInformation("Interview analysis API response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogWarning("API returned empty response despite success status");
                        return (false, null, "API returned empty response");
                    }

                    // Validate that the response is JSON
                    if (responseContent.TrimStart().StartsWith("<!DOCTYPE") || responseContent.TrimStart().StartsWith("<html"))
                    {
                        _logger.LogError("API returned HTML instead of JSON. Content: {Content}", responseContent);
                        return (false, null, "API returned HTML instead of JSON. This might be an error page.");
                    }

                    // Parse the JSON response
                    try
                    {
                        var analysisResponse = JsonSerializer.Deserialize<InterviewAnalysisResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        _logger.LogInformation("Successfully parsed interview analysis response");
                        return (true, analysisResponse, null);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError("API returned invalid JSON. Content: {Content}, Error: {Error}", responseContent, ex.Message);
                        return (false, null, $"API returned invalid JSON: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogError("Interview analysis API failed with status: {StatusCode}, content: {Content}", response.StatusCode, responseContent);
                    return (false, null, $"API call failed with status {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling interview analysis API");
                return (false, null, ex.Message);
            }
        }

        public async Task<InterviewAnalysisResult> SaveInterviewAnalysisResultAsync(int interviewSessionId, int userId, InterviewAnalysisResponse apiResponse)
        {
            try
            {
                _logger.LogInformation("Saving interview analysis result for session {SessionId} and user {UserId}", interviewSessionId, userId);

                var catalog = apiResponse.Response.Catalog;
                var summary = catalog.InterviewSummary;

                // Serialize roadmaps and tips to JSON
                var shortTermRoadmap = catalog.YourCareerRoadmaps.FirstOrDefault(r => r.Title.Contains("Short-Term"))?.Steps;
                var mediumTermRoadmap = catalog.YourCareerRoadmaps.FirstOrDefault(r => r.Title.Contains("Medium-Term"))?.Steps;
                var longTermRoadmap = catalog.YourCareerRoadmaps.FirstOrDefault(r => r.Title.Contains("Long-Term"))?.Steps;

                var analysisResult = new InterviewAnalysisResult
                {
                    InterviewSessionId = interviewSessionId,
                    UserId = userId,
                    Summary = summary.Summary,
                    Recommendations = summary.Recommendations,
                    MBAFocusArea = catalog.MBAFocusArea,
                    ClarityScore = catalog.ClarityScore,
                    YourCareerRoadmaps = catalog.YourCareerRoadmaps.Any() ? JsonSerializer.Serialize(catalog.YourCareerRoadmaps) : null,
                    AdditionalTips = catalog.AdditionalTips.Any() ? JsonSerializer.Serialize(catalog.AdditionalTips) : null,
                    RawApiResponse = JsonSerializer.Serialize(apiResponse),
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.InterviewAnalysisResults.Add(analysisResult);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved interview analysis result with ID {ResultId}", analysisResult.Id);
                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving interview analysis result");
                throw;
            }
        }

        public async Task<InterviewAnalysisResult?> GetInterviewAnalysisResultAsync(int interviewSessionId)
        {
            return await _dbContext.InterviewAnalysisResults
                .FirstOrDefaultAsync(ar => ar.InterviewSessionId == interviewSessionId);
        }

        public async Task<List<InterviewAnalysisResult>> GetUserInterviewAnalysisResultsAsync(int userId)
        {
            return await _dbContext.InterviewAnalysisResults
                .Where(ar => ar.UserId == userId)
                .OrderByDescending(ar => ar.CreatedAt)
                .ToListAsync();
        }
    }
}
