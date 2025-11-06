using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class GroupResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Url]
        public string Url { get; set; } = string.Empty;

        // Link to the group
        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

