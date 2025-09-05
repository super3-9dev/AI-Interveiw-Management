using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace InterviewBot.Pages.Account
{
    [Authorize]
    public class AccountModel : PageModel
    {
        public override bool TryValidateModel(object model)
        {
            // Disable automatic model validation - we'll handle it manually
            return true;
        }

        private readonly IProfileService _profileService;

        public AccountModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        // User Information - only bound when updating user info
        [BindProperty(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [BindProperty(Name = "FullName")]
        public string FullName { get; set; } = string.Empty;

        // Password Information - only bound when updating password
        [BindProperty(Name = "CurrentPassword")]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty(Name = "NewPassword")]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty(Name = "ConfirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Profile Information - only bound when updating profile
        [BindProperty(Name = "Strengths")]
        public string? Strengths { get; set; }

        [BindProperty(Name = "Weaknesses")]
        public string? Weaknesses { get; set; }

        [BindProperty(Name = "FutureCareerGoals")]
        public string? FutureCareerGoals { get; set; }

        [BindProperty(Name = "Interests")]
        public string? Interests { get; set; }

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
                        Strengths = profile.Strengths;
                        Weaknesses = profile.Weaknesses;
                        FutureCareerGoals = profile.FutureCareerGoals;
                        Interests = profile.Interests;
                    }
                    else
                    {
                        // Set null values if no profile exists
                        Strengths = null;
                        Weaknesses = null;
                        FutureCareerGoals = null;
                        Interests = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading profile: " + ex.Message;
            }
        }

        // Handler for updating user information (email and full name)
        public async Task<IActionResult> OnPostUpdateUserInfoAsync()
        {
            try
            {
                // Clear any existing validation errors for other sections
                ModelState.Clear();

                // Only validate user information fields
                if (string.IsNullOrWhiteSpace(Email))
                {
                    ModelState.AddModelError("Email", "Email is required");
                    return Page();
                }

                // Validate email format
                if (!Email.Contains("@") || !Email.Contains("."))
                {
                    ModelState.AddModelError("Email", "Invalid email format");
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(FullName))
                {
                    ModelState.AddModelError("FullName", "Full name is required");
                    return Page();
                }

                if (FullName.Length > 100)
                {
                    ModelState.AddModelError("FullName", "Full name cannot exceed 100 characters");
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Update user information
                var user = await _profileService.GetUserAsync(userId.Value);
                if (user != null)
                {
                    user.Email = Email;
                    user.FullName = FullName;

                    var userUpdateSuccess = await _profileService.UpdateUserAsync(user);
                    if (!userUpdateSuccess)
                    {
                        ErrorMessage = "Failed to update user information.";
                        return Page();
                    }

                    // Update the user's claims in the current session so the header shows the new name immediately
                    await UpdateUserClaimsAsync(user);

                    SuccessMessage = "User information updated successfully! The header will now display your new name.";
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating user information: " + ex.Message;
                return Page();
            }
        }

        // Handler for updating password
        public async Task<IActionResult> OnPostUpdatePasswordAsync()
        {
            try
            {
                // Clear any existing validation errors for other sections
                ModelState.Clear();

                // Only validate password fields
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    ViewData["CurrentPasswordError"] = "Current password is required";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    ViewData["NewPasswordError"] = "New password is required";
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Validate password change
                if (string.IsNullOrEmpty(NewPassword))
                {
                    ErrorMessage = "New password is required.";
                    return Page();
                }

                if (string.IsNullOrEmpty(CurrentPassword))
                {
                    ErrorMessage = "Current password is required to change password.";
                    return Page();
                }

                // Verify current password
                var currentUser = await _profileService.GetUserAsync(userId.Value);
                if (currentUser != null && !BCrypt.Net.BCrypt.Verify(CurrentPassword, currentUser.PasswordHash))
                {
                    ErrorMessage = "Current password is incorrect.";
                    return Page();
                }

                // Validate new password length
                if (NewPassword.Length < 6)
                {
                    ViewData["NewPasswordError"] = "New password must be at least 6 characters long.";
                    return Page();
                }

                if (NewPassword.Length > 100)
                {
                    ViewData["NewPasswordError"] = "New password cannot exceed 100 characters.";
                    return Page();
                }

                // Validate password confirmation
                if (NewPassword != ConfirmPassword)
                {
                    ViewData["ConfirmPasswordError"] = "Passwords do not match.";
                    return Page();
                }

                // Update password
                var user = await _profileService.GetUserAsync(userId.Value);
                if (user != null)
                {
                    var userUpdateSuccess = await _profileService.UpdateUserAsync(user, NewPassword);
                    if (!userUpdateSuccess)
                    {
                        ErrorMessage = "Failed to update password.";
                        return Page();
                    }

                    SuccessMessage = "Password updated successfully!";

                    // Clear sensitive fields
                    CurrentPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating password: " + ex.Message;
                return Page();
            }
        }

        // Handler for updating profile information
        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            try
            {
                // Clear any existing validation errors for other sections
                ModelState.Clear();

                // Validate profile field lengths if they contain data
                if (!string.IsNullOrWhiteSpace(Strengths) && Strengths.Length > 1000)
                {
                    ModelState.AddModelError("Strengths", "Strengths cannot exceed 1000 characters");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(Weaknesses) && Weaknesses.Length > 1000)
                {
                    ModelState.AddModelError("Weaknesses", "Weaknesses cannot exceed 1000 characters");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(FutureCareerGoals) && FutureCareerGoals.Length > 1000)
                {
                    ModelState.AddModelError("FutureCareerGoals", "Future career goals cannot exceed 1000 characters");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(Interests) && Interests.Length > 1000)
                {
                    ModelState.AddModelError("Interests", "Interests cannot exceed 1000 characters");
                    return Page();
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Update profile information
                var profiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var profile = profiles.FirstOrDefault();

                if (profile != null)
                {
                    // Update existing profile
                    profile.Strengths = Strengths ?? string.Empty;
                    profile.Weaknesses = Weaknesses ?? string.Empty;
                    profile.FutureCareerGoals = FutureCareerGoals ?? string.Empty;
                    profile.Interests = Interests ?? string.Empty;
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
                        Strengths = Strengths ?? string.Empty,
                        Weaknesses = Weaknesses ?? string.Empty,
                        FutureCareerGoals = FutureCareerGoals ?? string.Empty,
                        Interests = Interests ?? string.Empty,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _profileService.CreateProfileAsync(newProfile);
                }

                SuccessMessage = "Profile information updated successfully!";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating profile information: " + ex.Message;
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

        private async Task UpdateUserClaimsAsync(User user)
        {
            try
            {
                // Get the current user's identity
                var currentIdentity = User.Identity as ClaimsIdentity;
                if (currentIdentity != null)
                {
                    // Remove old claims
                    var oldNameClaim = currentIdentity.FindFirst(ClaimTypes.Name);
                    var oldEmailClaim = currentIdentity.FindFirst(ClaimTypes.Email);

                    if (oldNameClaim != null)
                        currentIdentity.RemoveClaim(oldNameClaim);
                    if (oldEmailClaim != null)
                        currentIdentity.RemoveClaim(oldEmailClaim);

                    // Add new claims
                    currentIdentity.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
                    currentIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

                    // The changes will be reflected immediately in the current request
                    // For subsequent requests, we need to refresh the authentication cookie
                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(currentIdentity));
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the update
                // The user information is still updated in the database
                System.Diagnostics.Debug.WriteLine($"Error updating user claims: {ex.Message}");
            }
        }
    }
}
