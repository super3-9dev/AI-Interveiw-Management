using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class InterviewCatalog
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Topic { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string InterviewType { get; set; } = string.Empty; // "Career Counselling" or "Purpose Discovery"

        [Required]
        public int AIAgentRoleId { get; set; }
        public AIAgentRole AIAgentRole { get; set; } = null!;



        [MaxLength(1000)]
        public string? KeyQuestions { get; set; } // Main questions for this interview type

        [MaxLength(500)]
        public string? TargetSkills { get; set; } // Skills this interview focuses on

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }
}
