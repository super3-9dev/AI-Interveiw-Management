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
        public string Introduction { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string InterviewType { get; set; } = string.Empty; // "Career Counselling" or "Purpose Discovery"

        [MaxLength(20)]
        public string Status { get; set; } = "NotStarted"; // "NotStarted", "InProgress", "Completed"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }
}
