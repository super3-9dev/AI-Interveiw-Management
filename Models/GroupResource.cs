using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class GroupResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string ResourceName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Url]
        public string Url { get; set; } = string.Empty;

        // Link to the group
        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed property for backward compatibility with UI (if needed)
        public string Title => ResourceName;
    }
}

