using Microsoft.EntityFrameworkCore;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.Extensions.Logging;

namespace InterviewBot.Services
{
    public interface IInterviewService
    {
        Task<IEnumerable<InterviewCatalog>> GenerateInterviewCatalogsAsync(int profileId, int userId);
        Task<InterviewCatalog> CreateCustomInterviewAsync(string title, string description, string customQuestions, string focusAreas, string difficultyLevel, string interviewDuration, int userId);
        Task<InterviewSession> StartInterviewAsync(int catalogId, int? customInterviewId, InterviewType type, int userId);
        Task<bool> SaveInterviewProgressAsync(int sessionId, int userId);
        Task<bool> FinishInterviewAsync(int sessionId, int userId);
        Task<bool> PauseInterviewAsync(int sessionId, int userId, string pauseReason);
        Task<bool> ResumeInterviewAsync(int sessionId, int userId);
        Task<InterviewSession?> GetInterviewSessionAsync(int sessionId, int userId);
        Task<IEnumerable<InterviewSession>> GetUserInterviewSessionsAsync(int userId);
        Task<IEnumerable<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId);
        Task<IEnumerable<CustomInterview>> GetUserCustomInterviewsAsync(int userId);
    }

    public class InterviewService : IInterviewService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<InterviewService> _logger;
        private readonly IExternalAPIService _externalAPIService;

        public InterviewService(AppDbContext dbContext, ILogger<InterviewService> logger, IExternalAPIService externalAPIService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _externalAPIService = externalAPIService;
        }

        // Implementation methods will be added here
        public async Task<IEnumerable<InterviewCatalog>> GenerateInterviewCatalogsAsync(int profileId, int userId)
        {
            try
            {
                _logger.LogInformation("Generating interview catalogs for profile {ProfileId} and user {UserId}", profileId, userId);

                // Get the profile to understand the user's background
                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId);

                if (profile == null)
                {
                    throw new ArgumentException("Profile not found");
                }

                // Call the external API to get interview catalog
                var catalogs = await CallExternalInterviewCatalogAPIAsync(userId);

                if (catalogs.Any())
                {
                    _dbContext.InterviewCatalogs.AddRange(catalogs);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully stored {Count} interview catalogs from external API", catalogs.Count());
                }

                return catalogs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating interview catalogs for profile {ProfileId}", profileId);
                throw;
            }
        }

        private async Task<IEnumerable<InterviewCatalog>> CallExternalInterviewCatalogAPIAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Calling external Interview Catalog API for user {UserId}", userId);

                // Create a simple request payload like in the image
                var interviewCatalogRequest = new
                {
                    // Simple request - the API will generate catalog based on this
                    // Based on the image, the API expects a simple request
                    request = "generate_interview_catalog"
                };

                // Call the external API service
                var result = await _externalAPIService.SendInterviewCatalogRequestAsync(interviewCatalogRequest);

                if (result.Success && result.Data != null)
                {
                    // Parse the API response and create InterviewCatalog objects
                    var catalogs = new List<InterviewCatalog>();

                    try
                    {
                        var catalogData = result.Data;

                        // Debug logging to see the actual response structure
                        _logger.LogInformation("API Response Data Keys: {Keys}", string.Join(", ", catalogData.Keys));

                        // First, try to use the pre-parsed catalogs from ExternalAPIService
                        if (catalogData.ContainsKey("parsedCatalogs") && catalogData["parsedCatalogs"] is object[] parsedCatalogs)
                        {
                            _logger.LogInformation("Found pre-parsed catalogs with {Count} items", parsedCatalogs.Length);

                            foreach (var item in parsedCatalogs)
                            {
                                if (item is Dictionary<string, object> itemData)
                                {
                                    _logger.LogInformation("Processing pre-parsed catalog item with keys: {Keys}", string.Join(", ", itemData.Keys));

                                    var catalog = new InterviewCatalog
                                    {
                                        UserId = userId,
                                        Topic = itemData.ContainsKey("topic") ? itemData["topic"]?.ToString() ?? "General Topic" : "General Topic",
                                        Introduction = itemData.ContainsKey("instruction") ? itemData["instruction"]?.ToString() ?? "General instruction" : "General instruction",
                                        InterviewType = "AI-Generated",

                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    catalogs.Add(catalog);
                                    _logger.LogInformation("Added catalog: {Topic}", catalog.Topic);
                                }
                            }

                            if (catalogs.Any())
                            {
                                _logger.LogInformation("Successfully processed {Count} pre-parsed catalogs", catalogs.Count);
                                return catalogs;
                            }
                        }

                        // Fallback to original parsing logic
                        if (catalogData.ContainsKey("response"))
                        {
                            _logger.LogInformation("Response key found, type: {Type}", catalogData["response"]?.GetType().Name);
                        }

                        // Handle the new API response format from the image
                        if (catalogData.ContainsKey("response") && catalogData["response"] is Dictionary<string, object> responseData)
                        {
                            _logger.LogInformation("Response data keys: {Keys}", string.Join(", ", responseData.Keys));

                            // Check if catalog exists and handle different array types
                            if (responseData.ContainsKey("catalog"))
                            {
                                var catalogValue = responseData["catalog"];
                                _logger.LogInformation("Catalog value type: {Type}", catalogValue?.GetType().Name);

                                // Handle different array types that might be returned
                                object[]? catalogArray = null;

                                if (catalogValue is object[] array)
                                {
                                    catalogArray = array;
                                    _logger.LogInformation("Direct array cast successful with {Count} items", array.Length);
                                }
                                else if (catalogValue is System.Collections.IEnumerable enumerable)
                                {
                                    try
                                    {
                                        // Convert IEnumerable to array with better error handling
                                        var tempList = new List<object>();
                                        foreach (var item in enumerable)
                                        {
                                            tempList.Add(item);
                                        }
                                        catalogArray = tempList.ToArray();
                                        _logger.LogInformation("Successfully converted IEnumerable to array with {Count} items", catalogArray.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error converting IEnumerable to array");
                                    }
                                }
                                else
                                {
                                    // Try to use reflection to get the array
                                    try
                                    {
                                        var type = catalogValue?.GetType();
                                        _logger.LogInformation("Attempting reflection-based conversion for type: {Type}", type?.Name);

                                        if (type != null && type.IsArray && catalogValue != null)
                                        {
                                            var length = ((Array)catalogValue).Length;
                                            _logger.LogInformation("Array length via reflection: {Length}", length);

                                            var tempList = new List<object>();
                                            for (int i = 0; i < length; i++)
                                            {
                                                var item = ((Array)catalogValue).GetValue(i);
                                                if (item != null)
                                                {
                                                    tempList.Add(item);
                                                }
                                            }
                                            catalogArray = tempList.ToArray();
                                            _logger.LogInformation("Successfully converted array via reflection with {Count} items", catalogArray.Length);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error in reflection-based array conversion");
                                    }
                                }

                                // Additional debug logging
                                if (catalogArray != null)
                                {
                                    _logger.LogInformation("Final catalog array type: {Type}, Length: {Length}",
                                        catalogArray.GetType().Name, catalogArray.Length);

                                    if (catalogArray.Length > 0)
                                    {
                                        _logger.LogInformation("First item type: {Type}", catalogArray[0]?.GetType().Name);
                                    }
                                }

                                if (catalogArray != null && catalogArray.Length > 0)
                                {
                                    _logger.LogInformation("Found catalog array with {Count} items", catalogArray.Length);

                                    foreach (var item in catalogArray)
                                    {
                                        _logger.LogInformation("Processing catalog item: {Item}", item);

                                        if (item is Dictionary<string, object> itemData)
                                        {
                                            _logger.LogInformation("Item data keys: {Keys}", string.Join(", ", itemData.Keys));

                                            var catalog = new InterviewCatalog
                                            {
                                                UserId = userId,
                                                Topic = itemData.ContainsKey("topic") ? itemData["topic"]?.ToString() ?? "General Topic" : "General Topic",
                                                Introduction = itemData.ContainsKey("instruction") ? itemData["instruction"]?.ToString() ?? "General instruction" : "General instruction",
                                                InterviewType = "AI-Generated",

                                                IsActive = true,
                                                CreatedAt = DateTime.UtcNow
                                            };
                                            catalogs.Add(catalog);
                                            _logger.LogInformation("Added catalog: {Topic}", catalog.Topic);
                                        }
                                    }

                                    _logger.LogInformation("Successfully parsed {Count} catalogs from external API response", catalogs.Count);
                                }
                                else
                                {
                                    _logger.LogWarning("Catalog array is empty or could not be converted to array");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Catalog key not found in response data");
                            }
                        }

                        // Alternative parsing for different response formats
                        if (catalogs.Count == 0 && catalogData.ContainsKey("catalog") && catalogData["catalog"] is object[] directCatalogArray)
                        {
                            foreach (var item in directCatalogArray)
                            {
                                if (item is Dictionary<string, object> itemData)
                                {
                                    var catalog = new InterviewCatalog
                                    {
                                        UserId = userId,
                                        Topic = itemData.ContainsKey("topic") ? itemData["topic"]?.ToString() ?? "General Topic" : "General Topic",
                                        Introduction = itemData.ContainsKey("instruction") ? itemData["instruction"]?.ToString() ?? "General instruction" : "General instruction",
                                        InterviewType = "AI-Generated",

                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    catalogs.Add(catalog);
                                }
                            }

                            if (catalogs.Any())
                            {
                                _logger.LogInformation("Successfully parsed {Count} catalogs from direct catalog array", catalogs.Count);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing external API response");
                    }

                    // Return the parsed catalogs (if any)
                    return catalogs;
                }
                else
                {
                    _logger.LogWarning("External API call failed: {ErrorMessage}", result.ErrorMessage);
                }

                // Return empty list if API call fails - no fallback to default catalogs
                return new List<InterviewCatalog>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external Interview Catalog API");
                return new List<InterviewCatalog>();
            }
        }



        public async Task<InterviewCatalog> CreateCustomInterviewAsync(string title, string description, string customQuestions, string focusAreas, string difficultyLevel, string interviewDuration, int userId)
        {
            try
            {
                _logger.LogInformation("Creating custom interview for user {UserId}", userId);

                var customInterview = new CustomInterview
                {
                    UserId = userId,
                    Title = title,
                    Description = description,
                    CustomQuestions = customQuestions,
                    FocusAreas = focusAreas,
                    DifficultyLevel = difficultyLevel,
                    InterviewDuration = interviewDuration,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.CustomInterviews.Add(customInterview);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created custom interview {InterviewId}", customInterview.Id);
                return new InterviewCatalog
                {
                    UserId = userId,
                    Topic = title,
                    Introduction = description,
                    InterviewType = "Custom",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom interview for user {UserId}", userId);
                throw;
            }
        }

        public async Task<InterviewSession> StartInterviewAsync(int catalogId, int? customInterviewId, InterviewType type, int userId)
        {
            try
            {
                _logger.LogInformation("Starting interview session for user {UserId}", userId);

                var session = new InterviewSession
                {
                    UserId = userId,
                    Type = type,
                    Status = InterviewStatus.InProgress,
                    StartTime = DateTime.UtcNow,
                    InterviewCatalogId = catalogId,
                    CustomInterviewId = customInterviewId,
                    CurrentQuestionNumber = 0,
                    IsCompleted = false,
                    Language = InterviewLanguage.English // Default to English
                };

                _dbContext.InterviewSessions.Add(session);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully started interview session {SessionId}", session.Id);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting interview session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SaveInterviewProgressAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Paused;
                session.PausedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} paused for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving interview progress for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> FinishInterviewAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Completed;
                session.EndTime = DateTime.UtcNow;
                session.IsCompleted = true;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} completed for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> PauseInterviewAsync(int sessionId, int userId, string pauseReason)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.Paused;
                session.PausedAt = DateTime.UtcNow;
                session.PauseReason = pauseReason;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} paused for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> ResumeInterviewAsync(int sessionId, int userId)
        {
            try
            {
                var session = await _dbContext.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

                if (session == null)
                    return false;

                session.Status = InterviewStatus.InProgress;
                session.ResumedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Interview session {SessionId} resumed for user {UserId}", sessionId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming interview session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<InterviewSession?> GetInterviewSessionAsync(int sessionId, int userId)
        {
            return await _dbContext.InterviewSessions
                .Include(s => s.InterviewCatalog)
                .Include(s => s.CustomInterview)
                .Include(s => s.AIAgentRole)
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<IEnumerable<InterviewSession>> GetUserInterviewSessionsAsync(int userId)
        {
            return await _dbContext.InterviewSessions
                .Include(s => s.InterviewCatalog)
                .Include(s => s.CustomInterview)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewCatalog>> GetUserInterviewCatalogsAsync(int userId)
        {
            return await _dbContext.InterviewCatalogs
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomInterview>> GetUserCustomInterviewsAsync(int userId)
        {
            return await _dbContext.CustomInterviews
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }


    }
}
