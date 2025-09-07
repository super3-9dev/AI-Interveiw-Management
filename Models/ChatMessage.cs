using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        [Required]
        public string InterviewId { get; set; } = null!;
        
        [MaxLength(10000)]
        public string? Question { get; set; }
        
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = null!;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}