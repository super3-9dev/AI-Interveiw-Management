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

        // AI Agent Role selection
        public int? SelectedAIAgentRoleId { get; set; }
        public AIAgentRole? SelectedAIAgentRole { get; set; }

        // Navigation properties
        public ICollection<Profile> Profiles { get; set; } = new List<Profile>();
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
        public ICollection<InterviewCatalog> InterviewCatalogs { get; set; } = new List<InterviewCatalog>();
        public ICollection<CustomInterview> CustomInterviews { get; set; } = new List<CustomInterview>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsGuest { get; set; } = false;
    }
}