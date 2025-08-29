using Microsoft.EntityFrameworkCore;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewBot.Services
{
    public interface IProfileService
    {
        Task<Profile> UploadAndAnalyzeResumeAsync(IFormFile file, int userId);
        Task<Profile> CreateProfileFromDescriptionAsync(string briefIntroduction, string careerGoals, string currentActivity, string motivations, int userId);
        Task<Profile?> GetProfileAsync(int id, int userId);
        Task<IEnumerable<Profile>> GetUserProfilesAsync(int userId);
        Task<bool> DeleteProfileAsync(int id, int userId);
        Task<bool> RetryAnalysisAsync(int analysisId, int userId);
        Task<string> GetAnalysisStatusAsync(int analysisId, int userId);
        Task<int> GetAnalysisProgressAsync(int analysisId, int userId);
    }

    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ProfileService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private static readonly Queue<int> _analysisQueue = new Queue<int>();
        private static readonly object _queueLock = new object();
        private static bool _isProcessing = false;

        public ProfileService(AppDbContext dbContext, ILogger<ProfileService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
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

                // Update status to processing
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 10);

                // Step 1: Document Processing (20%)
                await Task.Delay(1000); // Simulate document processing
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 30);

                // Step 2: Text Extraction (40%)
                await Task.Delay(1000); // Simulate text extraction
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 60);

                // Step 3: AI Analysis (60%)
                await Task.Delay(1000); // Simulate AI analysis
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 90);

                // Simulate AI analysis
                var analysisResult = await SimulateAIAnalysisAsync();

                // Update the profile with results
                var profile = await dbContext.Profiles.FindAsync(analysisId);
                if (profile != null)
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

                    profile.Status = "Completed";
                    profile.Progress = 100;
                    profile.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Profile {ProfileId} completed successfully", analysisId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing AI analysis for analysis {AnalysisId}", analysisId);
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
                _logger.LogError(ex, "Error updating profile status for {ProfileId}", analysisId);
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
    }
}
