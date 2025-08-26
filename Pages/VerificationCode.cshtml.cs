using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    public class VerificationCodeModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IStringLocalizer<VerificationCodeModel> _localizer;

        public VerificationCodeModel(AppDbContext db, IStringLocalizer<VerificationCodeModel> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string VerificationCode { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet(string email, string? subTopicId, string? culture)
        {
            Email = email ?? string.Empty;
            ErrorMessage = null;
            
            // Store subTopicId in session for later use
            if (!string.IsNullOrEmpty(subTopicId))
            {
                HttpContext.Session.SetString("SubTopicId", subTopicId);
            }

            // Set culture if provided
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append("culture", culture, new CookieOptions
                {
                    Path = "/",
                    Expires = DateTime.UtcNow.AddYears(1)
                });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(VerificationCode))
            {
                ErrorMessage = _localizer["Please enter both email and verification code."];
                return Page();
            }

            try
            {
                // Get the stored verification code from session
                var storedCode = HttpContext.Session.GetString($"VerificationCode_{Email}");
                var storedEmail = HttpContext.Session.GetString($"VerificationEmail_{Email}");
                var storedTime = HttpContext.Session.GetString($"VerificationTime_{Email}");

                if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(storedEmail))
                {
                    ErrorMessage = _localizer["Verification code not found. Please request a new code."];
                    return Page();
                }

                // Check if the email matches
                if (storedEmail != Email)
                {
                    ErrorMessage = _localizer["Email address mismatch."];
                    return Page();
                }

                // Check if the code has expired (10 minutes)
                if (DateTime.TryParse(storedTime, out var verificationTime))
                {
                    if (DateTime.UtcNow > verificationTime.AddMinutes(10))
                    {
                        ErrorMessage = _localizer["Verification code has expired. Please request a new code."];
                        return Page();
                    }
                }

                // Verify the code
                if (storedCode != VerificationCode)
                {
                    ErrorMessage = _localizer["Invalid verification code. Please try again."];
                    return Page();
                }

                // Code is valid - now check if user exists and handle accordingly
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email);
                
                if (user == null)
                {
                    // Create a new user account with default password
                    var defaultPassword = "Welcome123!"; // Default password for new users
                    user = new User
                    {
                        Email = Email,
                        FullName = Email.Split('@')[0], // Use email prefix as name
                        IsGuest = false,
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword)
                    };
                    
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();

                    // Set TempData to show alert on next page
                    TempData["ShowDefaultPasswordAlert"] = true;
                    TempData["DefaultPassword"] = defaultPassword;
                }
                else
                {
                    // Update last login time for existing user
                    user.LastLogin = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                // Create authentication claims
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Name, user.FullName),
                    new("IsGuest", user.IsGuest.ToString())
                };

                // Sign in the user automatically
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                    new AuthenticationProperties 
                    { 
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    });

                // Clear the verification data
                HttpContext.Session.Remove($"VerificationCode_{Email}");
                HttpContext.Session.Remove($"VerificationEmail_{Email}");
                HttpContext.Session.Remove($"VerificationTime_{Email}");

                // Get subTopicId from session
                var subTopicId = HttpContext.Session.GetString("SubTopicId");
                
                // Check if this is a public interview flow
                var publicInterviewEmail = HttpContext.Session.GetString("PublicInterviewEmail");
                var publicInterviewSubTopicId = HttpContext.Session.GetString("PublicInterviewSubTopicId");
                
                if (!string.IsNullOrEmpty(publicInterviewEmail) && Email == publicInterviewEmail)
                {
                    // This is a public interview flow - create session and redirect to regular Chat
                    return RedirectToPage("/PublicInterview", new { subTopicId = subTopicId, action = "createSession" });
                }
                else
                {
                    // Check if user was invited to a specific interview
                    if (string.IsNullOrEmpty(subTopicId))
                    {
                        // User was not invited to any specific interview
                        ErrorMessage = _localizer["You are not authorized to access any interview. Please contact the administrator or use a valid invitation link."];
                        return Page();
                    }

                    // Verify that the SubTopic exists and is published
                    var subTopic = await _db.SubTopics
                        .Include(st => st.Topic)
                        .FirstOrDefaultAsync(st => st.Id == int.Parse(subTopicId));

                    if (subTopic == null)
                    {
                        ErrorMessage = _localizer["The requested interview topic was not found."];
                        return Page();
                    }

                    if (!subTopic.IsPublished)
                    {
                        ErrorMessage = _localizer["The requested interview is not currently available."];
                        return Page();
                    }

                    // Regular flow - redirect to interview format selection with culture parameter
                    var culture = Request.Query["culture"].ToString();
                    if (!string.IsNullOrEmpty(culture))
                    {
                        return RedirectToPage("/InterviewFormat", new { email = Email, subTopicId = subTopicId, culture = culture });
                    }
                    return RedirectToPage("/InterviewFormat", new { email = Email, subTopicId = subTopicId });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = _localizer["An error occurred during verification. Please try again."];
                return Page();
            }
        }
    }
} 