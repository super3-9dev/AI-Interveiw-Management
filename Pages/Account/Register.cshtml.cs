using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InterviewBot.Pages.Account
{
    public class RegisterPageModel : PageModel  // Changed from RegisterModel to avoid conflict
    {
        private readonly AppDbContext _db;

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();  // Changed to RegisterInputModel

        public RegisterPageModel(AppDbContext db)
        {
            _db = db;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _db.Users.AnyAsync(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Email is already registered.");
                return Page();
            }

            var user = new User
            {
                Email = Input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                FullName = Input.FullName,
                Education = Input.Education,
                Experience = Input.Experience,
                CurrentPosition = Input.CurrentPosition,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }

    public class RegisterInputModel  // Separate model for input binding
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