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

        // External API Response
        [StringLength(5000)]
        public string? ExternalAPIResponse { get; set; }

        // Analysis Metadata
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        public int Progress { get; set; } = 0; // Progress percentage (0-100)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
