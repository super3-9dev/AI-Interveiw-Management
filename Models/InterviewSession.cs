// Models/InterviewSession.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{

    public enum InterviewLanguage
    {
        English,
        Spanish
    }

    public class InterviewSession
    {
        public int Id { get; set; }

        [Required]
        public int SubTopicId { get; set; }

        [ForeignKey("SubTopicId")]
        public SubTopic SubTopic { get; set; } = null!;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        [MaxLength(2000)]
        public string? Summary { get; set; }

        public List<ChatMessage> Messages { get; set; } = new();

        // Candidate information
        [MaxLength(100)]
        public string? CandidateName { get; set; }

        [MaxLength(100)]
        public string? CandidateEmail { get; set; }

        [MaxLength(100)]
        public string? CandidateEducation { get; set; }

        [MaxLength(50)]
        public string? CandidateExperience { get; set; }

        // Interview progress
        public int CurrentQuestionNumber { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;

        // Navigation property for results (one-to-one)
        public InterviewResult? Result { get; set; }

        // Convenience properties to access result data
        [NotMapped]
        public int? Score => Result?.Score;

        [NotMapped]
        public string? Evaluation => Result?.Evaluation;

        // Calculated property for duration
        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;

        [Required]
        public InterviewLanguage Language { get; set; } = InterviewLanguage.English;

        // Add to InterviewSession class
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}