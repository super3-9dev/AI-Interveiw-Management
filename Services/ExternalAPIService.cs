using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InterviewBot.Services
{
    public interface IExternalAPIService
    {
        Task<ExternalAPIResponse> SendAnalysisResultAsync(AnalysisData analysisData);
    }

    public class ExternalAPIService : IExternalAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalAPIService> _logger;
        private readonly string _webhookUrl;

        public ExternalAPIService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalAPIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _webhookUrl = configuration["ExternalAPI:WebhookUrl"] ?? "https://plataform.arandutechia.com/webhook/getAnalysisResult";

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "InterviewBot/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ExternalAPIResponse> SendAnalysisResultAsync(AnalysisData analysisData)
        {
            try
            {
                _logger.LogInformation("Sending analysis result to external API: {WebhookUrl}", _webhookUrl);
                _logger.LogInformation($"Analysis data - BriefIntro: {analysisData.BriefIntroduction?.Length ?? 0}, CareerGoals: {analysisData.FutureCareerGoals?.Length ?? 0}, CurrentActivities: {analysisData.CurrentActivities?.Length ?? 0}, Motivations: {analysisData.Motivations?.Length ?? 0}");

                var requestBody = new
                {
                    briefIntroduction = analysisData.BriefIntroduction,
                    futureCareerGoals = analysisData.FutureCareerGoals,
                    currentActivities = analysisData.CurrentActivities,
                    motivations = analysisData.Motivations
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Request payload: {Payload}", json);
                _logger.LogInformation($"Request content length: {content.Headers.ContentLength}");

                var response = await _httpClient.PostAsync(_webhookUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"External API response status: {response.StatusCode}, content length: {responseContent.Length}");
                _logger.LogInformation("External API response content: {Content}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("External API returned error: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);

                    return new ExternalAPIResponse
                    {
                        Success = false,
                        ErrorMessage = $"External API error: {response.StatusCode} - {responseContent}",
                        Data = null
                    };
                }

                // Try to parse the response
                try
                {
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    return new ExternalAPIResponse
                    {
                        Success = true,
                        ErrorMessage = null,
                        Data = responseData
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Could not parse external API response as JSON, treating as plain text");

                    return new ExternalAPIResponse
                    {
                        Success = true,
                        ErrorMessage = null,
                        Data = new Dictionary<string, object>
                        {
                            { "rawResponse", responseContent }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API");

                return new ExternalAPIResponse
                {
                    Success = false,
                    ErrorMessage = $"Error calling external API: {ex.Message}",
                    Data = null
                };
            }
        }
    }

    public class AnalysisData
    {
        public string BriefIntroduction { get; set; } = string.Empty;
        public string FutureCareerGoals { get; set; } = string.Empty;
        public string CurrentActivities { get; set; } = string.Empty;
        public string Motivations { get; set; } = string.Empty;
    }

    public class ExternalAPIResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }
}
