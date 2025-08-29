using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewBot.Services
{
    public interface IResumeAnalysisService
    {
        Task<ResumeAnalysis> UploadAndAnalyzeResumeAsync(IFormFile file, int userId);
        Task<ResumeAnalysis?> GetResumeAnalysisAsync(int id, int userId);
        Task<IEnumerable<ResumeAnalysis>> GetUserResumeAnalysesAsync(int userId);
        Task<bool> DeleteResumeAnalysisAsync(int id, int userId);
        Task<bool> RetryAnalysisAsync(int analysisId, int userId);
    }

    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ResumeAnalysisService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private static readonly Queue<int> _analysisQueue = new Queue<int>();
        private static readonly object _queueLock = new object();
        private static bool _isProcessing = false;

        public ResumeAnalysisService(AppDbContext dbContext, IWebHostEnvironment environment, ILogger<ResumeAnalysisService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _dbContext = dbContext;
            _environment = environment;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
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
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ResumeAnalyses.Add(resumeAnalysis);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Resume analysis record created with ID: {AnalysisId}", resumeAnalysis.Id);

                // Add to processing queue
                lock (_queueLock)
                {
                    _analysisQueue.Enqueue(resumeAnalysis.Id);
                    if (!_isProcessing)
                    {
                        _isProcessing = true;
                        _ = Task.Run(ProcessAnalysisQueueAsync);
                    }
                }

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

        public async Task<bool> RetryAnalysisAsync(int analysisId, int userId)
        {
            var analysis = await _dbContext.ResumeAnalyses
                .FirstOrDefaultAsync(r => r.Id == analysisId && r.UserId == userId);

            if (analysis == null)
                return false;

            // Reset status and add to queue
            analysis.Status = "Pending";
            analysis.ErrorMessage = null;
            analysis.UpdatedAt = DateTime.UtcNow;
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
                        await ProcessSingleAnalysisAsync(analysisId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing analysis {AnalysisId}", analysisId.Value);
                    }
                }
            }
        }

        private async Task ProcessSingleAnalysisAsync(int analysisId)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Use a new scope for each analysis to avoid context disposal issues
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Update status to processing
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 0);

                // Step 1: Document Processing (20%)
                await Task.Delay(1000); // Simulate document processing
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 20);

                // Step 2: Text Extraction (40%)
                await Task.Delay(1000); // Simulate text extraction
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 40);

                // Step 3: AI Analysis (60%)
                await Task.Delay(1000); // Simulate AI analysis
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 60);

                // Step 4: Skills Assessment (80%)
                await Task.Delay(1000); // Simulate skills assessment
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 80);

                // Step 5: Final Processing (100%)
                await Task.Delay(1000); // Simulate final processing
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Processing", null, 100);

                // Simulate AI analysis
                var analysisResult = await SimulateAIAnalysisAsync();

                // Update the analysis with results
                var analysis = await dbContext.ResumeAnalyses.FindAsync(analysisId);
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

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Analysis {AnalysisId} completed successfully", analysisId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing AI analysis for analysis {AnalysisId}", analysisId);
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await UpdateAnalysisStatusAsync(dbContext, analysisId, "Failed", ex.Message);
            }
        }

        private async Task UpdateAnalysisStatusAsync(AppDbContext dbContext, int analysisId, string status, string? errorMessage = null, int progress = 0)
        {
            try
            {
                var analysis = await dbContext.ResumeAnalyses.FindAsync(analysisId);
                if (analysis != null)
                {
                    analysis.Status = status;
                    analysis.ErrorMessage = errorMessage;
                    analysis.Progress = progress;
                    analysis.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
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

        private async Task<dynamic> SimulateAIAnalysisAsync()
        {
            // Simulate processing time - reduced from 2 seconds to 5 seconds for better UX
            await Task.Delay(5000);

            // Return more realistic mock analysis results
            return new
            {
                TopicsMarkdown = @"## Professional Profile

**Emrah Gunel** is a seasoned professional with extensive experience in software development and project management. His background demonstrates strong technical skills combined with leadership capabilities.

### Key Strengths:
- **Technical Expertise**: Proficient in multiple programming languages and frameworks
- **Project Management**: Experience leading development teams and delivering complex projects
- **Problem Solving**: Strong analytical skills with ability to troubleshoot and optimize systems
- **Communication**: Effective at bridging technical and business requirements

### Professional Summary:
Emrah has consistently demonstrated the ability to deliver high-quality solutions while maintaining focus on business objectives and user experience.",

                PossibleJobs = "Senior Software Developer, Technical Lead, Project Manager, Solutions Architect, DevOps Engineer, Product Manager",

                MbaSubjectsToReinforce = "Strategic Management, Business Analytics, Digital Transformation, Product Strategy, Change Management, Financial Analysis",

                BriefIntroduction = "Experienced software professional with a proven track record of delivering innovative solutions and leading technical teams. Combines deep technical knowledge with strong business acumen to drive successful project outcomes.",

                CurrentActivities = "Currently focused on developing scalable software solutions and mentoring junior developers. Actively involved in technology evaluation and architectural decision-making processes."
            };
        }
    }
}
