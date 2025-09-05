using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class Profile
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // AI Analysis Results
        [StringLength(1000)]
        public string? PossibleJobs { get; set; }

        [StringLength(1000)]
        public string? MbaSubjectsToReinforce { get; set; }

        [StringLength(1000)]
        public string? BriefIntroduction { get; set; }

        [StringLength(1000)]
        public string? CurrentActivities { get; set; }

        // Analysis Metadata
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        public int Progress { get; set; } = 0; // Progress percentage (0-100)

        // User Profile Information
        [StringLength(1000)]
        public string? Strengths { get; set; }

        [StringLength(1000)]
        public string? Weaknesses { get; set; }

        [StringLength(1000)]
        public string? FutureCareerGoals { get; set; }

        [StringLength(1000)]
        public string? Motivations { get; set; }

        [StringLength(1000)]
        public string? Interests { get; set; }

        [StringLength(1000)]
        public string? PotentialCareerPaths { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
