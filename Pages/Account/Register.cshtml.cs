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

            // Validate objective selection
            if (string.IsNullOrEmpty(Input.Objective))
            {
                ModelState.AddModelError("Input.Objective", "Please select your main objective.");
                return Page();
            }

            // Get the AI agent role based on the selected objective
            var aiAgentRole = await _db.AIAgentRoles
                .FirstOrDefaultAsync(r => r.RoleType == Input.Objective);

            if (aiAgentRole == null)
            {
                ModelState.AddModelError("Input.Objective", "Invalid objective selection.");
                return Page();
            }

            var user = new User
            {
                Email = Input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                FullName = Input.FullName,
                SelectedAIAgentRoleId = aiAgentRole.Id,
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
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;



        [Required]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your main objective.")]
        public string Objective { get; set; } = string.Empty; // "CareerCounselling" or "PurposeDiscovery"
    }
}