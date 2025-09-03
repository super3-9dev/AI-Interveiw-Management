using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class InterviewResult
    {
        [Key]
        public int Id { get; set; }

        // User information
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        // Interview catalog reference
        public string? InterviewId { get; set; }

        // Note: Temporarily using string to handle data type mismatch
        // Will be converted back to int after data cleanup

        // Interview information
        [Required]
        [MaxLength(200)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public DateTime CompleteDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}