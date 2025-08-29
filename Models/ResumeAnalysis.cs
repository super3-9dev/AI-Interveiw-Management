using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class ResumeAnalysis
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string FileHash { get; set; } = string.Empty;

        // AI Analysis Results
        [StringLength(4000)]
        public string? TopicsMarkdown { get; set; }

        [StringLength(1000)]
        public string? PossibleJobs { get; set; }

        [StringLength(1000)]
        public string? MbaSubjectsToReinforce { get; set; }

        [StringLength(1000)]
        public string? BriefIntroduction { get; set; }

        [StringLength(1000)]
        public string? CurrentActivities { get; set; }

        // Analysis Metadata
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public int Progress { get; set; } = 0; // Progress percentage (0-100)

        public TimeSpan? ProcessingTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
