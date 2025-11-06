using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // StudentCount is nullable in database but we treat it as non-nullable in code
        public int? StudentCount { get; set; }
        
        // Property to get StudentCount with default value of 0
        public int StudentCountValue => StudentCount ?? 0;

        // Link to the teacher who created the group
        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

