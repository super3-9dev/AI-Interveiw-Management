using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class GroupTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string AgentName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TaskName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Instructions { get; set; }

        [StringLength(255)]
        public string? Objective { get; set; }

        [StringLength(255)]
        public string? Constraints { get; set; }

        [StringLength(255)]
        public string? Emphasis { get; set; }

        // Link to the group
        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        // Task status (Visible/Hidden) - mapped to "IsVisible" column
        public bool IsVisible { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Computed property for Title (for backward compatibility with UI)
        public string Title => !string.IsNullOrWhiteSpace(TaskName) ? TaskName : AgentName;
    }
}

