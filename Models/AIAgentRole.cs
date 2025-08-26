using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class AIAgentRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RoleType { get; set; } = string.Empty; // "CareerCounselling" or "PurposeDiscovery"

        [StringLength(1000)]
        public string? Purpose { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
