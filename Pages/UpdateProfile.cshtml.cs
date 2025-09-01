using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;


namespace InterviewBot.Pages
{
    [Authorize]
    public class UpdateProfileModel : PageModel
    {
        private readonly IProfileService _profileService;

        public UpdateProfileModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(1000, ErrorMessage = "Strengths cannot exceed 1000 characters")]
        public string Strengths { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(1000, ErrorMessage = "Weaknesses cannot exceed 1000 characters")]
        public string Weaknesses { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(1000, ErrorMessage = "Career goals cannot exceed 1000 characters")]
        public string CareerGoals { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(1000, ErrorMessage = "Interests cannot exceed 1000 characters")]
        public string Interests { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    // Load actual user information from database
                    var user = await _profileService.GetUserAsync(userId.Value);
                    if (user != null)
                    {
                        Email = user.Email;
                        FullName = user.FullName;
                    }
                    else
                    {
                        // Fallback values if user not found
                        Email = "user@example.com";
                        FullName = "User Name";
                    }

                    // Load profile information if it exists
                    var profiles = await _profileService.GetUserProfilesAsync(userId.Value);
                    var profile = profiles.FirstOrDefault();
                    if (profile != null)
                    {
                        Strengths = profile.Strengths ?? string.Empty;
                        Weaknesses = profile.Weaknesses ?? string.Empty;
                        CareerGoals = profile.CareerGoals ?? string.Empty;
                        Interests = profile.Interests ?? string.Empty;
                    }
                    else
                    {
                        // Set empty values if no profile exists
                        Strengths = string.Empty;
                        Weaknesses = string.Empty;
                        CareerGoals = string.Empty;
                        Interests = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading profile: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Validate password change if new password is provided
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    if (string.IsNullOrEmpty(CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is required to change password.");
                        return Page();
                    }

                    // Verify current password
                    var currentUser = await _profileService.GetUserAsync(userId.Value);
                    if (currentUser != null && !BCrypt.Net.BCrypt.Verify(CurrentPassword, currentUser.PasswordHash))
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                        return Page();
                    }

                    // Validate new password length
                    if (NewPassword.Length < 6)
                    {
                        ModelState.AddModelError("NewPassword", "New password must be at least 6 characters long.");
                        return Page();
                    }

                    // Validate password confirmation
                    if (NewPassword != ConfirmPassword)
                    {
                        ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                        return Page();
                    }
                }

                // Update user information
                var user = await _profileService.GetUserAsync(userId.Value);
                if (user != null)
                {
                    user.Email = Email;
                    user.FullName = FullName;

                    // Pass the new password if provided
                    var userUpdateSuccess = await _profileService.UpdateUserAsync(user, NewPassword);
                    if (!userUpdateSuccess)
                    {
                        ErrorMessage = "Failed to update user information.";
                        return Page();
                    }
                }

                // Update profile information
                var profiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var profile = profiles.FirstOrDefault();

                if (profile != null)
                {
                    // Update existing profile
                    profile.Strengths = Strengths;
                    profile.Weaknesses = Weaknesses;
                    profile.CareerGoals = CareerGoals;
                    profile.Interests = Interests;
                    profile.UpdatedAt = DateTime.UtcNow;

                    var profileUpdateSuccess = await _profileService.UpdateProfileAsync(profile);
                    if (!profileUpdateSuccess)
                    {
                        ErrorMessage = "Failed to update profile information.";
                        return Page();
                    }
                }
                else
                {
                    // Create new profile if none exists
                    var newProfile = new Profile
                    {
                        UserId = userId.Value,
                        Strengths = Strengths,
                        Weaknesses = Weaknesses,
                        CareerGoals = CareerGoals,
                        Interests = Interests,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _profileService.CreateProfileAsync(newProfile);
                }

                var updateMessage = "Profile updated successfully!";
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    updateMessage += " Password has been changed.";
                }
                SuccessMessage = updateMessage;

                // Clear sensitive fields
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating profile: " + ex.Message;
                return Page();
            }
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
