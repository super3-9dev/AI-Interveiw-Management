using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class CustomInterview
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? CustomQuestions { get; set; } // JSON array of custom questions
        
        [MaxLength(500)]
        public string? FocusAreas { get; set; } // Areas of focus for this interview
        
        [MaxLength(100)]
        public string? DifficultyLevel { get; set; } // Beginner, Intermediate, Advanced
        
        [MaxLength(100)]
        public string? InterviewDuration { get; set; } // Estimated duration
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }
}
