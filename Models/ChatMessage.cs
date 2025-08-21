using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int SessionId { get; set; }
        
        [ForeignKey("SessionId")]
        public InterviewSession Session { get; set; } = null!;
        
        public bool IsUserMessage { get; set; }
        
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}