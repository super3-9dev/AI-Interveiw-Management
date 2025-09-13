using InterviewBot.Models;
using InterviewBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace InterviewBot.Services
{
    public class StudentReportService : IStudentReportService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StudentReportService> _logger;
        private readonly AppDbContext _context;

        public StudentReportService(HttpClient httpClient, ILogger<StudentReportService> logger, AppDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
        }


        public async Task<StudentReportResponse?> GetStudentReportAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Calling getStudentReport API for userId: {UserId}", userId);

                var requestBody = new
                {
                    userId = userId
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://plataform.arandutechia.com/webhook/getStudentReport", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("response====================>" + responseContent);
                    _logger.LogInformation("API response received successfully");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<StudentReportResponse>(responseContent, options);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed with status: {StatusCode}, Error: {ErrorContent}", response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling getStudentReport API for userId: {UserId}", userId);
                return null;
            }
        }
    }
}
