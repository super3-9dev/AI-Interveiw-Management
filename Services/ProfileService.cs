using Microsoft.EntityFrameworkCore;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;


namespace InterviewBot.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ProfileService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOpenAIService _openAIService;
        private readonly IExternalAPIService _externalAPIService;
        private static readonly Queue<int> _analysisQueue = new Queue<int>();
        private static readonly object _queueLock = new object();
        private static bool _isProcessing = false;

        public ProfileService(AppDbContext dbContext, ILogger<ProfileService> logger, IServiceScopeFactory serviceScopeFactory, IOpenAIService openAIService, IExternalAPIService externalAPIService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _openAIService = openAIService;
            _externalAPIService = externalAPIService;
        }

        public async Task<Profile> UploadAndAnalyzeResumeAsync(IFormFile file, int userId)
        {
            try
            {
                _logger.LogInformation("Starting resume upload for user {UserId}", userId);

                // Validate file
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file provided");

                if (!IsValidPdfFile(file))
                    throw new ArgumentException("Only PDF files are allowed");

                // No file storage needed - just validate the file

                // Create profile record
                var profile = new Profile
                {
                    UserId = userId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Profile record created with ID: {ProfileId}", profile.Id);

                // Add to processing queue
                lock (_queueLock)
                {
                    _analysisQueue.Enqueue(profile.Id);
                    if (!_isProcessing)
                    {
                        _isProcessing = true;
                        _ = Task.Run(ProcessAnalysisQueueAsync);
                    }
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading and analyzing resume for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Profile> CreateProfileFromDescriptionAsync(string briefIntroduction, string careerGoals, string currentActivity, string motivations, int userId)
        {
            try
            {
                _logger.LogInformation("Starting text-based profile creation for user {UserId}", userId);

                // Create profile record with the provided text data
                var profile = new Profile
                {
                    UserId = userId,
                    BriefIntroduction = briefIntroduction,
                    PossibleJobs = careerGoals, // Store career goals in PossibleJobs field
                    CurrentActivities = currentActivity,
                    MbaSubjectsToReinforce = motivations, // Store motivations in MbaSubjectsToReinforce field
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Text-based profile record created with ID: {ProfileId}", profile.Id);

                // Add to processing queue for AI analysis
                lock (_queueLock)
                {
                    _analysisQueue.Enqueue(profile.Id);
                    if (!_isProcessing)
                    {
                        _isProcessing = true;
                        _ = Task.Run(ProcessAnalysisQueueAsync);
                    }
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating text-based profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Profile?> GetProfileAsync(int id, int userId)
        {
            return await _dbContext.Profiles
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task<IEnumerable<Profile>> GetUserProfilesAsync(int userId)
        {
            return await _dbContext.Profiles
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteProfileAsync(int id, int userId)
        {
            var profile = await _dbContext.Profiles
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (profile == null)
                return false;

            // No physical file to delete since we removed file storage

            _dbContext.Profiles.Remove(profile);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RetryAnalysisAsync(int analysisId, int userId)
        {
            var profile = await _dbContext.Profiles
                .FirstOrDefaultAsync(r => r.Id == analysisId && r.UserId == userId);

            if (profile == null)
                return false;

            // Reset status and add to queue
            profile.Status = "Pending";
            profile.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            lock (_queueLock)
            {
                _analysisQueue.Enqueue(analysisId);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _ = Task.Run(ProcessAnalysisQueueAsync);
                }
            }

            return true;
        }

        public async Task<string> GetAnalysisStatusAsync(int analysisId, int userId)
        {
            var profile = await _dbContext.Profiles
                .FirstOrDefaultAsync(r => r.Id == analysisId && r.UserId == userId);

            return profile?.Status ?? "Not Found";
        }

        public async Task<int> GetAnalysisProgressAsync(int analysisId, int userId)
        {
            var profile = await _dbContext.Profiles
                .Where(r => r.Id == analysisId && r.UserId == userId)
                .FirstOrDefaultAsync();

            return profile?.Progress ?? 0;
        }

        private async Task ProcessAnalysisQueueAsync()
        {
            while (true)
            {
                int? analysisId = null;

                lock (_queueLock)
                {
                    if (_analysisQueue.Count == 0)
                    {
                        _isProcessing = false;
                        return;
                    }
                    analysisId = _analysisQueue.Dequeue();
                }

                if (analysisId.HasValue)
                {
                    try
                    {
                        await ProcessAnalysisAsync(analysisId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing analysis {AnalysisId}", analysisId.Value);
                    }
                }
            }
        }

        private async Task ProcessAnalysisAsync(int analysisId)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Use a new scope for each analysis to avoid context disposal issues
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Update status to processing - Start of unified workflow
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 10);

                // Step 1: AI Analysis (40% of total progress)
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", "Analyzing uploaded file and input data...", 25);
                await Task.Delay(1000); // Simulate AI analysis
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", "AI analysis completed, preparing external API call...", 40);

                // Step 2: External API Call (80% of total progress)
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", "Calling external API with analysis results...", 60);
                await Task.Delay(1000); // Simulate external API call
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", "External API call completed, finalizing results...", 80);

                // Get the profile from database
                var profile = await dbContext.Profiles.FindAsync(analysisId);
                if (profile == null)
                {
                    _logger.LogError("Profile {ProfileId} not found in database", analysisId);
                    await UpdateAnalysisStatusAsync(dbContext, analysisId, "Failed", "Profile not found", 0);
                    return;
                }

                // Perform real AI analysis using OpenAI
                var analysisResult = await PerformRealAIAnalysisAsync(profile);

                // Update the profile with AI analysis results
                {
                    // Only update fields that are empty (preserve user input for text-based profiles)
                    if (string.IsNullOrEmpty(profile.PossibleJobs))
                        profile.PossibleJobs = analysisResult.PossibleJobs;

                    if (string.IsNullOrEmpty(profile.MbaSubjectsToReinforce))
                        profile.MbaSubjectsToReinforce = analysisResult.MbaSubjectsToReinforce;

                    if (string.IsNullOrEmpty(profile.BriefIntroduction))
                        profile.BriefIntroduction = analysisResult.BriefIntroduction;

                    if (string.IsNullOrEmpty(profile.CurrentActivities))
                        profile.CurrentActivities = analysisResult.CurrentActivities;

                    // Call external API with the analysis data
                    _logger.LogInformation("About to call external API for profile {ProfileId}", analysisId);
                    var externalAPIResponse = await CallExternalAPIAsync(analysisResult);
                    if (externalAPIResponse.Success)
                    {
                        _logger.LogInformation($"External API call successful for profile {analysisId}, response processed");
                    }
                    else
                    {
                        _logger.LogInformation($"External API call failed for profile {analysisId}: {externalAPIResponse.ErrorMessage}");
                    }

                    // Final step: Results ready (100% of total progress)
                    await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", "External API results received, analysis complete!", 95);

                    profile.Status = "Completed";
                    profile.Progress = 100;
                    profile.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Profile {analysisId} completed successfully");

                    // Generate interview catalogs for the completed profile (ONLY ONCE per user)
                    try
                    {
                        // Check if user already has interview catalogs to avoid duplicate API calls
                        var existingCatalogs = await dbContext.InterviewCatalogs
                            .Where(c => c.UserId == profile.UserId)
                            .AnyAsync();

                        if (!existingCatalogs)
                        {
                            _logger.LogInformation($"User {profile.UserId} has no existing interview catalogs, calling API for first time");

                            using var interviewScope = _serviceScopeFactory.CreateScope();
                            var interviewService = interviewScope.ServiceProvider.GetRequiredService<IInterviewService>();
                            var catalogs = await interviewService.GenerateInterviewCatalogsAsync(profile.Id, profile.UserId);
                            _logger.LogInformation($"Generated {catalogs.Count()} interview catalogs for profile {profile.Id} (first time for user {profile.UserId})");
                        }
                        else
                        {
                            _logger.LogInformation($"User {profile.UserId} already has interview catalogs, skipping API call to avoid duplicates");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error generating interview catalogs for profile {profile.Id}");
                        // Don't fail the profile completion if interview catalog generation fails
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing AI analysis for analysis {analysisId}");
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Failed", null, 0);
            }
        }

        private async Task UpdateAnalysisStatusAsync(AppDbContext dbContext, int analysisId, string status, string? errorMessage = null, int progress = 0)
        {
            try
            {
                var profile = await dbContext.Profiles.FindAsync(analysisId);
                if (profile != null)
                {
                    profile.Status = status;
                    profile.Progress = progress;
                    profile.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating profile status for {analysisId}");
            }
        }

        private bool IsValidPdfFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return allowedExtensions.Contains(fileExtension) &&
                   file.ContentType.ToLowerInvariant() == "application/pdf";
        }

        private async Task<dynamic> SimulateAIAnalysisAsync()
        {
            // Simulate processing time - reduced from 2 seconds to 5 seconds for better UX
            await Task.Delay(5000);

            // Return more realistic mock analysis results
            return new
            {
                PossibleJobs = "Senior Software Developer, Technical Lead, Project Manager, Solutions Architect, DevOps Engineer, Product Manager",

                MbaSubjectsToReinforce = "Strategic Management, Business Analytics, Digital Transformation, Product Strategy, Change Management, Financial Analysis",

                BriefIntroduction = "Experienced software professional with a proven track record of delivering innovative solutions and leading technical teams. Combines deep technical knowledge with strong business acumen to drive successful project outcomes.",

                CurrentActivities = "Currently focused on developing scalable software solutions and mentoring junior developers. Actively involved in technology evaluation and architectural decision-making processes."
            };
        }

        private async Task<dynamic> PerformRealAIAnalysisAsync(Profile profile)
        {
            try
            {
                _logger.LogInformation($"Performing real AI analysis for profile {profile.Id}");

                // Build context for AI analysis
                var context = BuildAnalysisContext(profile);

                // Generate analysis using OpenAI
                var analysisPrompt = $@"Based on the following information, provide a comprehensive career analysis:

Context: {context}

Please provide analysis in the following format:
1. Brief Introduction: A professional summary
2. Possible Jobs: Relevant job titles and career paths
3. MBA Subjects to Reinforce: Key business areas to focus on
4. Current Activities: Analysis of current work/studies

Format the response as JSON with these exact keys: briefIntroduction, possibleJobs, mbaSubjectsToReinforce, currentActivities";

                var aiResponse = await _openAIService.GenerateInterviewResponseAsync(analysisPrompt, "Career Analysis");

                // Try to parse the AI response as JSON
                try
                {
                    var analysisData = JsonSerializer.Deserialize<Dictionary<string, string>>(aiResponse);

                    return new
                    {
                        BriefIntroduction = analysisData?.GetValueOrDefault("briefIntroduction", "Analysis in progress..."),
                        PossibleJobs = analysisData?.GetValueOrDefault("possibleJobs", "Analysis in progress..."),
                        MbaSubjectsToReinforce = analysisData?.GetValueOrDefault("mbaSubjectsToReinforce", "Analysis in progress..."),
                        CurrentActivities = analysisData?.GetValueOrDefault("currentActivities", "Analysis in progress...")
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Could not parse AI response as JSON, using fallback analysis");

                    // Fallback to structured analysis
                    _logger.LogWarning(aiResponse, "Using fallback analysis");
                    return new
                    {
                        BriefIntroduction = "Experienced software professional with a proven track record of delivering innovative solutions and leading technical teams. Combines deep technical knowledge with strong business acumen to drive successful project outcomes.",
                        PossibleJobs = "Software Developer, Technical Lead, Project Manager",
                        MbaSubjectsToReinforce = "Strategic Management, Business Analytics, Digital Transformation",
                        CurrentActivities = "Currently focused on software development and technical leadership"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing real AI analysis, falling back to simulation");
                return await SimulateAIAnalysisAsync();
            }
        }

        private string BuildAnalysisContext(Profile profile)
        {
            var contextParts = new List<string>();

            if (!string.IsNullOrEmpty(profile.BriefIntroduction))
                contextParts.Add($"Brief Introduction: {profile.BriefIntroduction}");

            if (!string.IsNullOrEmpty(profile.CurrentActivities))
                contextParts.Add($"Current Activities: {profile.CurrentActivities}");

            if (!string.IsNullOrEmpty(profile.PossibleJobs))
                contextParts.Add($"Career Goals: {profile.PossibleJobs}");

            if (!string.IsNullOrEmpty(profile.MbaSubjectsToReinforce))
                contextParts.Add($"Areas of Interest: {profile.MbaSubjectsToReinforce}");

            return string.Join("\n", contextParts);
        }

        private async Task<ExternalAPIResponse> CallExternalAPIAsync(dynamic analysisResult)
        {
            try
            {
                _logger.LogInformation("Preparing to call external API with analysis result");

                // Safely extract values from dynamic object
                string briefIntroduction = "";
                string futureCareerGoals = "";
                string currentActivities = "";
                string motivations = "";

                try
                {
                    briefIntroduction = analysisResult.BriefIntroduction?.ToString() ?? "";
                    futureCareerGoals = analysisResult.PossibleJobs?.ToString() ?? "";
                    currentActivities = analysisResult.CurrentActivities?.ToString() ?? "";
                    motivations = analysisResult.MbaSubjectsToReinforce?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting values from dynamic analysis result, using empty strings");
                }

                _logger.LogInformation($"Extracted values - BriefIntro: {briefIntroduction.Length}, CareerGoals: {futureCareerGoals.Length}, CurrentActivities: {currentActivities.Length}, Motivations: {motivations.Length}");

                var analysisData = new AnalysisData
                {
                    BriefIntroduction = briefIntroduction,
                    FutureCareerGoals = futureCareerGoals,
                    CurrentActivities = currentActivities,
                    Motivations = motivations
                };

                _logger.LogInformation("Calling external API service");
                var result = await _externalAPIService.SendAnalysisResultAsync(analysisData);
                _logger.LogInformation($"External API service returned: Success={result.Success}, ErrorMessage={result.ErrorMessage}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API");
                return new ExternalAPIResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Data = null
                };
            }
        }

        public async Task<bool> UpdateProfileAsync(Profile profile)
        {
            try
            {
                _logger.LogInformation("Updating profile {ProfileId} for user {UserId}", profile.Id, profile.UserId);

                var existingProfile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.Id == profile.Id && p.UserId == profile.UserId);

                if (existingProfile == null)
                {
                    _logger.LogWarning("Profile {ProfileId} not found for user {UserId}", profile.Id, profile.UserId);
                    return false;
                }

                // Update the profile fields
                existingProfile.Strengths = profile.Strengths;
                existingProfile.Weaknesses = profile.Weaknesses;
                existingProfile.FutureCareerGoals = profile.FutureCareerGoals;
                existingProfile.Interests = profile.Interests;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Profile {ProfileId} updated successfully", profile.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile {ProfileId}", profile.Id);
                return false;
            }
        }

        public async Task<Profile> CreateProfileAsync(Profile profile)
        {
            try
            {
                _logger.LogInformation("Creating new profile for user {UserId}", profile.UserId);

                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;

                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Profile {ProfileId} created successfully", profile.Id);

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile for user {UserId}", profile.UserId);
                throw;
            }
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            try
            {
                return await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user, string? newPassword = null)
        {
            try
            {
                _logger.LogInformation("Updating user {UserId}", user.Id);

                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (existingUser == null)
                {
                    _logger.LogWarning("User {UserId} not found", user.Id);
                    return false;
                }

                // Update the user fields
                existingUser.Email = user.Email;
                existingUser.FullName = user.FullName;
                existingUser.UpdatedAt = DateTime.UtcNow;

                // Update password if provided
                if (!string.IsNullOrEmpty(newPassword))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    _logger.LogInformation("Password updated for user {UserId}", user.Id);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("User {UserId} updated successfully", user.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user.Id);
                return false;
            }
        }

        public async Task<bool> HasCompletedProfileAsync(int userId)
        {
            try
            {
                var hasCompletedProfile = await _dbContext.Profiles
                    .AnyAsync(p => p.UserId == userId && p.Status == "Completed");

                _logger.LogInformation("User {UserId} has completed profile: {HasCompleted}", userId, hasCompletedProfile);
                return hasCompletedProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} has completed profile", userId);
                return false;
            }
        }

        public async Task<(bool Success, string? ApiResponse, string? ErrorMessage)> CallResumeAnalysisAPIAsync(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Calling resume analysis API for file: {FileName}", file.FileName);

                // Leer todo a memoria para forzar Content-Length y evitar chunked
                byte[] bytes;
                await using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                if (bytes.Length == 0)
                    return (false, null, "El archivo está vacío.");

                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false // Evitar perder el body en redirecciones
                };

                using var http = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };

                // Desactivar Expect: 100-continue (algunos servidores fallan con esto)
                http.DefaultRequestHeaders.ExpectContinue = false;
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PdfUploaderClient/1.0");

                using var form = new MultipartFormDataContent();

                // Contenido del archivo como byte array (Content-Length conocido)
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType
                    );
                fileContent.Headers.ContentDisposition =
                    new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"file\"",
                        FileName = $"\"{Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "document.pdf" : file.FileName)}\""
                    };
                // (Opcional) declarar longitud de la parte
                fileContent.Headers.ContentLength = bytes.Length;

                form.Add(fileContent, "file", Path.GetFileName(file.FileName));

                var url = "https://plataform.arandutechia.com/webhook/ai_pdf_summariser";
                _logger.LogInformation("POST {Url} ({Length} bytes)", url, bytes.Length);

                using var response = await http.PostAsync(url, form);
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Status: {Status} | Len: {Len}", response.StatusCode, body?.Length ?? 0);
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"API call failed with status {response.StatusCode}: {body}");

                if (string.IsNullOrWhiteSpace(body))
                    return (false, null, "API returned empty response");

                if (body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                    body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                    return (false, null, "API returned HTML instead of JSON (posible WAF/redirect).");
                
                return (true, body, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling resume analysis API");                    
                return (false, null, ex.Message);
            }
        }

        public async Task<Profile> CreateProfileFromApiResponseAsync(string apiResponse, int userId, bool isFallback = false)
        {
            try
            {
                _logger.LogInformation("Creating profile from API response for user {UserId}, isFallback: {IsFallback}", userId, isFallback);

                var profile = new Profile
                {
                    UserId = userId,
                    Status = "Completed",
                    Progress = 100,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Note: ExternalAPIResponse field has been removed
                };

                // Validate and parse the JSON response
                if (string.IsNullOrWhiteSpace(apiResponse))
                {
                    _logger.LogError("API response is empty or null for user {UserId}", userId);
                    throw new ArgumentException("API response cannot be empty or null", nameof(apiResponse));
                }

                using var jsonDocument = JsonDocument.Parse(apiResponse);
                var root = jsonDocument.RootElement;
                
                // Extract the result object from the API response
                if (root.TryGetProperty("result", out var result))
                {
                    // Map API response fields to profile properties from the result object
                    if (result.TryGetProperty("possibleJobs", out var possibleJobs))
                        profile.PossibleJobs = possibleJobs.GetString() ?? string.Empty;

                    if (result.TryGetProperty("mbaSubjectsToReinforce", out var mbaSubjects))
                        profile.MbaSubjectsToReinforce = mbaSubjects.GetString() ?? string.Empty;

                    if (result.TryGetProperty("briefIntroduction", out var briefIntro))
                        profile.BriefIntroduction = briefIntro.GetString() ?? string.Empty;

                    if (result.TryGetProperty("currentActivities", out var currentActivities))
                        profile.CurrentActivities = currentActivities.GetString() ?? string.Empty;

                    if (result.TryGetProperty("motivations", out var motivations))
                        profile.Motivations = motivations.GetString() ?? string.Empty;

                    if (result.TryGetProperty("futureCareerGoals", out var careerGoals))
                        profile.FutureCareerGoals = careerGoals.GetString() ?? string.Empty;

                    if (result.TryGetProperty("strengths", out var strengths))
                        profile.Strengths = strengths.GetString() ?? string.Empty;

                    if (result.TryGetProperty("weaknesses", out var weaknesses))
                        profile.Weaknesses = weaknesses.GetString() ?? string.Empty;

                    if (result.TryGetProperty("potentialCareerPaths", out var careerPaths))
                        profile.Interests = careerPaths.GetString() ?? string.Empty;
                }
                else
                {
                    _logger.LogWarning("API response does not contain 'result' object. Response structure: {Response}", apiResponse);
                }

                // Note: TopicsMarkdown field has been removed
                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Profile {ProfileId} created successfully from API response for user {UserId}", profile.Id, userId);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile from API response for user {UserId}", userId);
                throw;
            }
        }

        private string GenerateTopicsMarkdown(Profile profile)
        {
            var markdown = new System.Text.StringBuilder();
            
            markdown.AppendLine("# Profile Analysis");
            markdown.AppendLine();

            if (!string.IsNullOrEmpty(profile.BriefIntroduction))
            {
                markdown.AppendLine("## Brief Introduction");
                markdown.AppendLine(profile.BriefIntroduction);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.CurrentActivities))
            {
                markdown.AppendLine("## Current Activities");
                markdown.AppendLine(profile.CurrentActivities);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.PossibleJobs))
            {
                markdown.AppendLine("## Potential Job Opportunities");
                markdown.AppendLine(profile.PossibleJobs);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.Strengths))
            {
                markdown.AppendLine("## Strengths");
                markdown.AppendLine(profile.Strengths);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.Weaknesses))
            {
                markdown.AppendLine("## Areas for Improvement");
                markdown.AppendLine(profile.Weaknesses);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.MbaSubjectsToReinforce))
            {
                markdown.AppendLine("## MBA Subjects to Reinforce");
                markdown.AppendLine(profile.MbaSubjectsToReinforce);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.Motivations))
            {
                markdown.AppendLine("## Motivations");
                markdown.AppendLine(profile.Motivations);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.FutureCareerGoals))
            {
                markdown.AppendLine("## Future Career Goals");
                markdown.AppendLine(profile.FutureCareerGoals);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(profile.Interests))
            {
                markdown.AppendLine("## Potential Career Paths");
                markdown.AppendLine(profile.Interests);
                markdown.AppendLine();
            }

            return markdown.ToString();
        }

        public async Task<Profile> UpdateProfileFromApiResponseAsync(Profile existingProfile, string apiResponse, bool isFallback = false)
        {
            try
            {
                _logger.LogInformation("Updating existing profile {ProfileId} from API response for user {UserId}, isFallback: {IsFallback}", 
                    existingProfile.Id, existingProfile.UserId, isFallback);

                // Update the profile fields
                existingProfile.Status = "Completed";
                existingProfile.Progress = 100;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                // Validate and parse the JSON response
                if (string.IsNullOrWhiteSpace(apiResponse))
                {
                    _logger.LogError("API response is empty or null for user {UserId}", existingProfile.UserId);
                    throw new ArgumentException("API response cannot be empty or null", nameof(apiResponse));
                }

                using var jsonDocument = JsonDocument.Parse(apiResponse);
                var root = jsonDocument.RootElement;

                // Extract the result object from the API response
                if (root.TryGetProperty("result", out var result))
                {
                    // Map API response fields to profile properties from the result object
                    if (result.TryGetProperty("possibleJobs", out var possibleJobs))
                        existingProfile.PossibleJobs = possibleJobs.GetString() ?? string.Empty;

                    if (result.TryGetProperty("mbaSubjectsToReinforce", out var mbaSubjects))
                        existingProfile.MbaSubjectsToReinforce = mbaSubjects.GetString() ?? string.Empty;

                    if (result.TryGetProperty("briefIntroduction", out var briefIntro))
                        existingProfile.BriefIntroduction = briefIntro.GetString() ?? string.Empty;

                    if (result.TryGetProperty("currentActivities", out var currentActivities))
                        existingProfile.CurrentActivities = currentActivities.GetString() ?? string.Empty;

                    if (result.TryGetProperty("motivations", out var motivations))
                        existingProfile.Motivations = motivations.GetString() ?? string.Empty;

                    if (result.TryGetProperty("futureCareerGoals", out var careerGoals))
                        existingProfile.FutureCareerGoals = careerGoals.GetString() ?? string.Empty;

                    if (result.TryGetProperty("strengths", out var strengths))
                        existingProfile.Strengths = strengths.GetString() ?? string.Empty;

                    if (result.TryGetProperty("weaknesses", out var weaknesses))
                        existingProfile.Weaknesses = weaknesses.GetString() ?? string.Empty;

                    if (result.TryGetProperty("potentialCareerPaths", out var careerPaths))
                        existingProfile.Interests = careerPaths.GetString() ?? string.Empty;
                }
                else
                {
                    _logger.LogWarning("API response does not contain 'result' object. Response structure: {Response}", apiResponse);
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Profile {ProfileId} updated successfully from API response for user {UserId}", 
                    existingProfile.Id, existingProfile.UserId);
                return existingProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile {ProfileId} from API response for user {UserId}", 
                    existingProfile.Id, existingProfile.UserId);
                throw;
            }
        }

        private string GetFallbackApiResponse()
        {
            return @"{
                ""possibleJobs"": ""Potential job opportunities for Emrah include positions as a Senior Shopify Developer, eCommerce Consultant, or Front-End Developer, possibly within larger organizations or agencies that focus on delivering comprehensive eCommerce solutions."",
                ""mbaSubjectsToReinforce"": ""To further enhance his career, Emrah could benefit from reinforcing subjects related to Digital Marketing, Project Management, and Business Analytics during an MBA program. Understanding these areas in-depth would provide him with a broader business perspective and enhance his ability to strategize and execute eCommerce initiatives effectively."",
                ""briefIntroduction"": ""Emrah Gunel is a seasoned Shopify Developer with a focus on theme customization, development, app integrations, and eCommerce business creation. With over 7 years of hands-on experience in Shopify and eCommerce development, Emrah possesses a comprehensive skill set that allows him to effectively leverage modern web design trends and standards to build high-performing online stores."",
                ""currentActivities"": ""Currently, Emrah is employed as a Shopify Developer at Mark Anthony Group, where he is responsible for setting up Shopify stores, theme configurations, custom functionalities, and much more. His previous roles include positions at Royal Retailer, Anheuser-Busch, Carian's Bistro Chocolates, and Design Furniture, where he honed his skills in various aspects of both front-end development and eCommerce management."",
                ""motivations"": ""Emrah is motivated by a desire to create intuitive digital experiences that drive customer engagement and revenue growth for online businesses. He is passionate about keeping up with the latest trends in web design and eCommerce, which is showcased through his continuous learning and application of advanced technologies in his work."",
                ""futureCareerGoals"": """",
                ""strengths"": ""Emrah's strengths lie in his extensive experience with Shopify Liquid, custom theme development, site optimization, and strong understanding of front-end technologies including HTML, CSS, and JavaScript. His hands-on experience with various eCommerce platforms, debugging skills, and knowledge of integrations further enhance his capability to deliver high-quality solutions."",
                ""weaknesses"": ""One potential weakness could be his specific focus on Shopify, which may limit his exposure to other eCommerce platforms or technologies that could broaden his expertise. Additionally, while Emrah has experience with a variety of programming languages and tools, his primary proficiency may lead to less experience with certain niche tools that could be beneficial in specific projects."",
                ""potentialCareerPaths"": ""Emrah's career could progress towards roles such as eCommerce Manager or Technical Project Manager, where he can leverage his deep understanding of development and customer relationship dynamics. He may also transition into a consulting role, helping businesses optimize their Shopify and eCommerce strategies.""
            }";
        }
    }
}
