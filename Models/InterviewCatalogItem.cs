using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewBot.Models
{
    public class InterviewCatalogItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Topic { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Instruction { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? InterviewType { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }
}
