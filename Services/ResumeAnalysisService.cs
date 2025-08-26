using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace InterviewBot.Services
{
    public interface IResumeAnalysisService
    {
        Task<ResumeAnalysis> UploadAndAnalyzeResumeAsync(IFormFile file, int userId);
        Task<ResumeAnalysis?> GetResumeAnalysisAsync(int id, int userId);
        Task<IEnumerable<ResumeAnalysis>> GetUserResumeAnalysesAsync(int userId);
        Task<bool> DeleteResumeAnalysisAsync(int id, int userId);
    }

    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ResumeAnalysisService> _logger;

        public ResumeAnalysisService(AppDbContext dbContext, IWebHostEnvironment environment, ILogger<ResumeAnalysisService> logger)
        {
            _dbContext = dbContext;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ResumeAnalysis> UploadAndAnalyzeResumeAsync(IFormFile file, int userId)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Starting resume upload for user {UserId}", userId);
                
                // Validate file
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file provided");

                if (!IsValidPdfFile(file))
                    throw new ArgumentException("Only PDF files are allowed");

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                
                // Ensure directory exists
                if (!Directory.Exists(uploadPath))
                {
                    _logger.LogInformation("Creating upload directory: {UploadPath}", uploadPath);
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);
                _logger.LogInformation("Saving file to: {FilePath}", filePath);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Calculate file hash
                var fileHash = await CalculateFileHashAsync(filePath);
                _logger.LogInformation("File hash calculated: {FileHash}", fileHash);

                // Create resume analysis record
                var resumeAnalysis = new ResumeAnalysis
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileHash = fileHash,
                    Status = "Processing",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ResumeAnalyses.Add(resumeAnalysis);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Resume analysis record created with ID: {AnalysisId}", resumeAnalysis.Id);

                // Start AI analysis in background
                _ = Task.Run(async () => await PerformAIAnalysisAsync(resumeAnalysis.Id, filePath));

                return resumeAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading and analyzing resume for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ResumeAnalysis?> GetResumeAnalysisAsync(int id, int userId)
        {
            return await _dbContext.ResumeAnalyses
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task<IEnumerable<ResumeAnalysis>> GetUserResumeAnalysesAsync(int userId)
        {
            return await _dbContext.ResumeAnalyses
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteResumeAnalysisAsync(int id, int userId)
        {
            var analysis = await _dbContext.ResumeAnalyses
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (analysis == null)
                return false;

            // Delete physical file
            if (File.Exists(analysis.FilePath))
            {
                try
                {
                    File.Delete(analysis.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete physical file: {FilePath}", analysis.FilePath);
                }
            }

            _dbContext.ResumeAnalyses.Remove(analysis);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private async Task PerformAIAnalysisAsync(int analysisId, string filePath)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Update status to processing
                await UpdateAnalysisStatusAsync(analysisId, "Processing");

                // TODO: Integrate with actual AI service (OpenAI, etc.)
                // For now, we'll simulate the analysis
                var analysisResult = await SimulateAIAnalysisAsync(filePath);

                // Update the analysis with results
                var analysis = await _dbContext.ResumeAnalyses.FindAsync(analysisId);
                if (analysis != null)
                {
                    analysis.TopicsMarkdown = analysisResult.TopicsMarkdown;
                    analysis.PossibleJobs = analysisResult.PossibleJobs;
                    analysis.MbaSubjectsToReinforce = analysisResult.MbaSubjectsToReinforce;
                    analysis.BriefIntroduction = analysisResult.BriefIntroduction;
                    analysis.CurrentActivities = analysisResult.CurrentActivities;
                    analysis.Status = "Completed";
                    analysis.AnalysisDate = DateTime.UtcNow;
                    analysis.ProcessingTime = DateTime.UtcNow - startTime;
                    analysis.UpdatedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing AI analysis for analysis {AnalysisId}", analysisId);
                await UpdateAnalysisStatusAsync(analysisId, "Failed", ex.Message);
            }
        }

        private async Task UpdateAnalysisStatusAsync(int analysisId, string status, string? errorMessage = null)
        {
            try
            {
                var analysis = await _dbContext.ResumeAnalyses.FindAsync(analysisId);
                if (analysis != null)
                {
                    analysis.Status = status;
                    analysis.ErrorMessage = errorMessage;
                    analysis.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis status for {AnalysisId}", analysisId);
            }
        }

        private bool IsValidPdfFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            return allowedExtensions.Contains(fileExtension) && 
                   file.ContentType.ToLowerInvariant() == "application/pdf";
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task<dynamic> SimulateAIAnalysisAsync(string filePath)
        {
            // Simulate processing time
            await Task.Delay(2000);

            // Return mock analysis results
            return new
            {
                TopicsMarkdown = "## Professional Profile\n\nExperienced professional with expertise in...",
                PossibleJobs = "Software Developer, Project Manager, Business Analyst",
                MbaSubjectsToReinforce = "Digital Marketing, Product Management, UI/UX Design",
                BriefIntroduction = "Experienced professional with strong technical and analytical skills...",
                CurrentActivities = "Currently working on innovative projects in technology sector..."
            };
        }
    }
}
