using InterviewBot.Data;
using InterviewBot.Models;
using InterviewBot.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InterviewBot.Pages
{
    public class EmailVerificationModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerificationModel> _logger;
        private readonly IStringLocalizer<EmailVerificationModel> _localizer;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string VerificationCode { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsLoginMode { get; set; } = false;

        public EmailVerificationModel(AppDbContext db, IEmailService emailService, ILogger<EmailVerificationModel> logger, IStringLocalizer<EmailVerificationModel> localizer)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
            _localizer = localizer;
        }

        public void OnGet(int? subTopicId, string? mode, string? culture)
        {
            // Clear any previous messages
            ErrorMessage = null;
            SuccessMessage = null;
            
            // Set login mode if requested
            IsLoginMode = mode == "login";
            
            // Store subTopicId in session for later use
            if (subTopicId.HasValue)
            {
                HttpContext.Session.SetString("SubTopicId", subTopicId.Value.ToString());
            }

            // Pre-fill email if coming from PublicInterview page
            var publicInterviewEmail = HttpContext.Session.GetString("PublicInterviewEmail");
            if (!string.IsNullOrEmpty(publicInterviewEmail))
            {
                Email = publicInterviewEmail;
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
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = _localizer["Please enter a valid email address."];
                return Page();
            }

            try
            {
                // Generate a 6-digit verification code
                var verificationCode = GenerateVerificationCode();
                
                // Store the verification code in session (you might want to use a database table for production)
                HttpContext.Session.SetString($"VerificationCode_{Email}", verificationCode);
                HttpContext.Session.SetString($"VerificationEmail_{Email}", Email);
                HttpContext.Session.SetString($"VerificationTime_{Email}", DateTime.UtcNow.ToString("O"));

                // Get culture from query string or cookie
                var culture = Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(culture))
                {
                    culture = Request.Cookies["culture"];
                }

                // Send verification email
                await _emailService.SendVerificationEmailAsync(Email, verificationCode, culture);

                SuccessMessage = _localizer["Verification code has been sent to your email address."];
                
                // Get subTopicId from session
                var subTopicId = HttpContext.Session.GetString("SubTopicId");
                
                // Store email in session for verification code page
                HttpContext.Session.SetString("VerificationEmail", Email);
                
                // Redirect to verification code entry page with culture parameter
                if (!string.IsNullOrEmpty(culture))
                {
                    return RedirectToPage("/VerificationCode", new { email = Email, subTopicId = subTopicId, culture = culture });
                }
                return RedirectToPage("/VerificationCode", new { email = Email, subTopicId = subTopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", Email);
                ErrorMessage = _localizer["Failed to send verification code. Please try again."];
                return Page();
            }
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(VerificationCode))
            {
                ErrorMessage = _localizer["Please enter both email and verification code."];
                IsLoginMode = true;
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
                    IsLoginMode = true;
                    return Page();
                }

                // Check if the email matches
                if (storedEmail != Email)
                {
                    ErrorMessage = _localizer["Email address mismatch."];
                    IsLoginMode = true;
                    return Page();
                }

                // Check if the code has expired (10 minutes)
                if (DateTime.TryParse(storedTime, out var verificationTime))
                {
                    if (DateTime.UtcNow > verificationTime.AddMinutes(10))
                    {
                        ErrorMessage = _localizer["Verification code has expired. Please request a new code."];
                        IsLoginMode = true;
                        return Page();
                    }
                }

                // Verify the code
                if (storedCode != VerificationCode)
                {
                    ErrorMessage = _localizer["Invalid verification code. Please try again."];
                    IsLoginMode = true;
                    return Page();
                }

                // Code is valid - find or create user
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email);
                if (user == null)
                {
                    // Create new user if doesn't exist
                    user = new User
                    {
                        Email = Email,
                        FullName = Email.Split('@')[0], // Use email prefix as full name
                        PasswordHash = "", // Empty for verification code login
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }

                // Create authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Clear the verification data
                HttpContext.Session.Remove($"VerificationCode_{Email}");
                HttpContext.Session.Remove($"VerificationEmail_{Email}");
                HttpContext.Session.Remove($"VerificationTime_{Email}");

                // Get subTopicId from session for redirect
                var subTopicId = HttpContext.Session.GetString("SubTopicId");
                
                if (!string.IsNullOrEmpty(subTopicId))
                {
                    // Create interview session and redirect to regular chat (not public chat)
                    try
                    {
                        // Create a new interview session
                        var session = new InterviewSession
                        {
                            SubTopicId = int.Parse(subTopicId),
                            CandidateEmail = Email,
                            StartTime = DateTime.UtcNow,
                            Language = 0, // Default language
                            IsCompleted = false
                        };

                        _db.InterviewSessions.Add(session);
                        await _db.SaveChangesAsync();

                        // Store session info in session
                        HttpContext.Session.SetString("CurrentSessionId", session.Id.ToString());
                        HttpContext.Session.SetString("InterviewFormat", "text");

                        // Clear verification data
                        HttpContext.Session.Remove("SubTopicId");
                        HttpContext.Session.Remove("PublicInterviewEmail");
                        HttpContext.Session.Remove("PublicInterviewSubTopicId");

                        // Redirect to regular chat page (not public chat) with culture parameter
                        var culture = Request.Query["culture"].ToString();
                        if (!string.IsNullOrEmpty(culture))
                        {
                            return RedirectToPage("/Chat", new { subTopicId = int.Parse(subTopicId), sessionId = session.Id, culture = culture });
                        }
                        return RedirectToPage("/Chat", new { subTopicId = int.Parse(subTopicId), sessionId = session.Id });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create interview session for {Email}", Email);
                        return RedirectToPage("/Index");
                    }
                }
                else
                {
                    // Redirect to dashboard for regular login
                    return RedirectToPage("/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to login with verification code for {Email}", Email);
                ErrorMessage = _localizer["An error occurred during login. Please try again."];
                IsLoginMode = true;
                return Page();
            }
        }
    }
} 