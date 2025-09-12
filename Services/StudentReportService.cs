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

        public async Task<ComprehensiveReportResponse?> GetComprehensiveReportAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Calling comprehensive report API for userId: {UserId}", userId);

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
                    _logger.LogInformation("Comprehensive API response received successfully");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<ComprehensiveReportResponse>(responseContent, options);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Comprehensive API call failed with status: {StatusCode}, Error: {ErrorContent}", response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling comprehensive report API for userId: {UserId}", userId);
                return null;
            }
        }

        public async Task<StudentReportResponse?> GetStudentReportAsync(string userId)
        {
            // Declare variables outside try block for use in catch
            string fullName = "";
            List<object> interviewData = new List<object>();
            string processDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            try
            {
                
                if (int.TryParse(userId, out int userIdInt))
                {
                    // Get topic from InterviewResults table using InterviewSessionId
                    var topics = await _context.InterviewResults
                        .Where(i => i.Id == userIdInt)
                        .Select(i => new { i.Topic, i.Id })
                        .ToListAsync();
                    Console.WriteLine("topics====================>" + topics);
                    // Get interviews from InterviewAnalysisResults table
                    var interviewResults = await _context.InterviewAnalysisResults
                        .Where(i => i.UserId == userIdInt)
                        .ToListAsync();
                    
                    _logger.LogInformation("Found {Count} interview results for userId: {UserId}", interviewResults.Count, userId);
                    
                    // Loop through each interview and get suitable data
                    foreach (var interviewResult in interviewResults)
                    {
                        var topic = topics.FirstOrDefault(t => t.Id == interviewResult.InterviewSessionId)?.Topic ?? "Unknown Interview";
                        
                        var interviewDataItem = new
                        {
                            title = topic,
                            date = interviewResult.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            status = "Completed", // Since it's from InterviewResults, it's completed
                            summary = interviewResult.Summary ?? "No summary available"
                        };
                        
                        interviewData.Add(interviewDataItem);
                        _logger.LogInformation("Processed interview: {InterviewSessionId} - {Title}", interviewResult.InterviewSessionId, interviewDataItem.title);
                    }
                }
                try
                {
                    if (userIdInt > 0)
                    {
                        var user = await _context.Users
                            .Where(u => u.Id == userIdInt)
                            .Select(u => u.FullName)
                            .FirstOrDefaultAsync();
                        
                        if (!string.IsNullOrEmpty(user))
                        {
                            fullName = user;
                            _logger.LogInformation("Found user in database: {FullName} for userId: {UserId}", fullName, userId);
                        }
                        else
                        {
                            _logger.LogWarning("User not found in database for userId: {UserId}", userId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid userId format: {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching user from database for userId: {UserId}", userId);
                }
                
                // If no user found in database, use a default name
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = "Unknown User";
                    _logger.LogWarning("Using default name 'Unknown User' for userId: {UserId}", userId);
                }

                // Create the request body matching the API structure from the Postman image
                var requestBody = new
                {
                    studentData = new
                    {
                        userId = userId,
                        fullName = fullName,
                        processDate = processDate,
                        interviews = interviewData // Use actual interview data from database
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling getStudentReport API for userId: {UserId}, fullName: {FullName}, processDate: {ProcessDate}", 
                    userId, fullName, processDate);

                var response = await _httpClient.PostAsync("https://plataform.arandutechia.com/webhook/getStudentReport", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API response received successfully====================>");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    Console.WriteLine("result====================>" + responseContent);
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
                
                // Return a fallback response with local data if API fails
                return new StudentReportResponse
                {
                    StudentData = new StudentData
                    {
                        UserId = userId,
                        FullName = fullName,
                        ProcessDate = processDate,
                        Interviews = interviewData.Select(item => new InterviewData
                        {
                            Title = ((dynamic)item).title,
                            Date = ((dynamic)item).date,
                            Status = ((dynamic)item).status,
                            Summary = ((dynamic)item).summary
                        }).ToList()
                    }
                };
            }
        }
    }
}
