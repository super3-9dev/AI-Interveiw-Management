using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace InterviewBot.Models
{
    public class SubTopic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }
        
        [Required]
        [MultipleEmailAddress]
        public string CandidateEmail { get; set; } = null!;

        public bool IsPublished { get; set; } = false;

        public int TopicId { get; set; }
        public Topic Topic { get; set; } = null!;
        public List<InterviewSession> InterviewSessions { get; set; } = new();
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

    public class MultipleEmailAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Email is required.");
            }

            var emailString = value.ToString()!;
            var emails = emailString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            if (emails.Length == 0)
            {
                return new ValidationResult("At least one email address is required.");
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            foreach (var email in emails)
            {
                var trimmedEmail = email.Trim();
                if (string.IsNullOrWhiteSpace(trimmedEmail))
                {
                    return new ValidationResult("Empty email addresses are not allowed.");
                }

                if (!emailRegex.IsMatch(trimmedEmail))
                {
                    return new ValidationResult($"Invalid email format: {trimmedEmail}");
                }
            }

            return ValidationResult.Success;
        }
    }
}