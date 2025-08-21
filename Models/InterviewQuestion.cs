using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class InterviewQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Answer { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Score { get; set; }

        [MaxLength(2000)]
        public string Feedback { get; set; } = string.Empty;

        [Required]
        public int InterviewResultId { get; set; }

        [ForeignKey("InterviewResultId")]
        public InterviewResult Result { get; set; } = null!;
    }
}