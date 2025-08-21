using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;  // Initialize with default

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Education { get; set; }  // Nullable
        public string? Experience { get; set; }
        public string? CurrentPosition { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsGuest { get; set; } = false;
    }
}