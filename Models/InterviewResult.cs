using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class InterviewResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public InterviewSession Session { get; set; } = null!;

        [Range(0, 100)]
        public int? Score { get; set; }

        [Required]
        public string Evaluation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<InterviewQuestion> Questions { get; set; } = new();
    }
}