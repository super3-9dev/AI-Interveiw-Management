using System.Text;
using System.Text.Json;
using InterviewBot.Models;
using InterviewBot.Data;
using Microsoft.EntityFrameworkCore;

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
                _logger.LogInformation("Calling analysis API for interview: {InterviewId}", request.InterviewId);

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                _logger.LogInformation("API Request Payload: {Payload}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://plataform.arandutechia.com/webhook/getAnalysisResult", content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var analysisResponse = JsonSerializer.Deserialize<InterviewAnalysisResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        return (true, analysisResponse, null);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize API response");
                        return (false, null, $"Failed to parse API response: {ex.Message}");
                    }
                }
                else
                {
                    return (false, null, $"API call failed with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling analysis API for interview: {InterviewId}", request.InterviewId);
                return (false, null, ex.Message);
            }
        }

        public async Task<InterviewAnalysisResult> SaveInterviewAnalysisResultAsync(int interviewSessionId, int userId, InterviewAnalysisResponse apiResponse)
        {
            try
            {
                var result = new InterviewAnalysisResult
                {
                    InterviewSessionId = interviewSessionId,
                    UserId = userId,
                    RawApiResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { WriteIndented = true }),
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.InterviewAnalysisResults.Add(result);
                await _dbContext.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving interview analysis result");
                throw;
            }
        }

        public async Task<InterviewAnalysisResult?> GetInterviewAnalysisResultAsync(int interviewSessionId)
        {
            try
            {
                return await _dbContext.InterviewAnalysisResults
                    .FirstOrDefaultAsync(r => r.InterviewSessionId == interviewSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview analysis result for session: {SessionId}", interviewSessionId);
                return null;
            }
        }

        public async Task<List<InterviewAnalysisResult>> GetUserInterviewAnalysisResultsAsync(int userId)
        {
            try
            {
                return await _dbContext.InterviewAnalysisResults
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user interview analysis results for user: {UserId}", userId);
                return new List<InterviewAnalysisResult>();
            }
        }
    }
}
