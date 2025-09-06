using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class InterviewAnalysisResult
    {
        public int Id { get; set; }

        public int? InterviewSessionId { get; set; }
        public InterviewSession? InterviewSession { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Interview Summary
        [StringLength(2000)]
        public string? Summary { get; set; }

        [StringLength(2000)]
        public string? Recommendations { get; set; }

        [StringLength(100)]
        public string? MBAFocusArea { get; set; }

        public int ClarityScore { get; set; }

        // Career Roadmaps (stored as JSON)
        public string? YourCareerRoadmaps { get; set; }

        // Additional Tips (stored as JSON)
        public string? AdditionalTips { get; set; }

        // Raw API Response (for debugging/backup)
        public string? RawApiResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
