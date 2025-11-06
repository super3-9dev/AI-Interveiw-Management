using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    [Authorize]
    public class MyGroupsModel : PageModel
    {
        private readonly AppDbContext _db;

        public MyGroupsModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Group> Groups { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(200, ErrorMessage = "Group name cannot exceed 200 characters")]
        public string GroupName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string GroupDescription { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is a Teacher (idProfile = 1)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 1)
            {
                // Redirect non-teachers to home page
                return RedirectToPage("/Index");
            }

            await LoadGroupsAsync();
            return Page();
        }


        public async Task<IActionResult> OnPostCreateGroupAsync()
        {
            // Check if user is a Teacher (idProfile = 1)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 1)
            {
                // Redirect non-teachers to home page
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync();
                ErrorMessage = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    await LoadGroupsAsync();
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Create new group in database
                var newGroup = new Group
                {
                    Name = GroupName,
                    Description = GroupDescription,
                    UserId = userId.Value,
                    StudentCount = 0, // Will be set to 0 by default
                    CreatedAt = DateTime.UtcNow
                };

                _db.Groups.Add(newGroup);
                await _db.SaveChangesAsync();

                // Clear form
                GroupName = string.Empty;
                GroupDescription = string.Empty;

                SuccessMessage = "Group created successfully!";
                
                // Redirect to refresh the page and show the new group
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }
                
                return RedirectToPage("/MyGroups", new { culture = currentCulture });
            }
            catch (Exception ex)
            {
                await LoadGroupsAsync();
                ErrorMessage = "Error creating group: " + ex.Message;
                return Page();
            }
        }

        private async Task LoadGroupsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                Groups = new List<Group>();
                return;
            }

            // Load groups created by the current user (teacher)
            Groups = await _db.Groups
                .Where(g => g.UserId == userId.Value)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}

