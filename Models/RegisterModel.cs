// Models/RegisterModel.cs
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Models
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords don't match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Education { get; set; }
        public string? Experience { get; set; }
        public string? CurrentPosition { get; set; }
    }
}