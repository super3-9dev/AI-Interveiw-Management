using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class InterviewNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InterviewId { get; set; }

        [Required]
        [StringLength(2000)]
        public string ActionTaken { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [StringLength(2000)]
        public string? AdditionalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("InterviewId")]
        public InterviewResult? InterviewResult { get; set; }
    }
}
