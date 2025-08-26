using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    public class InterviewFormatModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IStringLocalizer<InterviewFormatModel> _localizer;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public int SubTopicId { get; set; }

        public string? ErrorMessage { get; set; }
        public SubTopic? SubTopic { get; set; }
        public bool ShowDefaultPasswordAlert { get; set; } = false;

        public InterviewFormatModel(AppDbContext db, IStringLocalizer<InterviewFormatModel> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<IActionResult> OnGetAsync(string email, int? subTopicId, string? culture)
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/EmailVerification");
            }

            // Get email from authenticated user
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/EmailVerification");
            }

            Email = userEmail;
            SubTopicId = subTopicId ?? 0;

            if (SubTopicId > 0)
            {
                SubTopic = await _db.SubTopics
                    .Include(st => st.Topic)
                    .FirstOrDefaultAsync(st => st.Id == SubTopicId);
            }

            // Check for default password alert
            if (TempData["ShowDefaultPasswordAlert"] != null && (bool)TempData["ShowDefaultPasswordAlert"])
            {
                ShowDefaultPasswordAlert = true;
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

            return Page();
        }

        public async Task<IActionResult> OnPostTextInterviewAsync()
        {
            return await StartInterview("text");
        }

        public async Task<IActionResult> OnPostVoiceInterviewAsync()
        {
            return await StartInterview("voice");
        }

        private async Task<IActionResult> StartInterview(string format)
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/EmailVerification");
            }

            // Get email from authenticated user
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/EmailVerification");
            }

            Email = userEmail;

            try
            {
                // Check if user has already completed this interview
                var existingSession = await _db.InterviewSessions
                    .Where(s => s.SubTopicId == SubTopicId && s.CandidateEmail == Email && s.IsCompleted)
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    ErrorMessage = _localizer["You have already completed this interview. You cannot access it again."];
                    return Page();
                }

                // Create a new interview session
                var session = new InterviewSession
                {
                    SubTopicId = SubTopicId,
                    CandidateEmail = Email,
                    StartTime = DateTime.UtcNow,
                    Language = 0, // Default language
                    IsCompleted = false
                };

                _db.InterviewSessions.Add(session);
                await _db.SaveChangesAsync();

                // Store session info in session
                HttpContext.Session.SetString("CurrentSessionId", session.Id.ToString());
                HttpContext.Session.SetString("InterviewFormat", format);

                // Get culture parameter for redirect
                var culture = Request.Query["culture"].ToString();
                
                if (format == "text")
                {
                    // Redirect to text chat with culture parameter
                    if (!string.IsNullOrEmpty(culture))
                    {
                        return RedirectToPage("/Chat", new { subTopicId = SubTopicId, sessionId = session.Id, culture = culture });
                    }
                    return RedirectToPage("/Chat", new { subTopicId = SubTopicId, sessionId = session.Id });
                }
                else
                {
                    // Redirect to voice interview with culture parameter
                    if (!string.IsNullOrEmpty(culture))
                    {
                        return RedirectToPage("/VoiceInterview", new { subTopicId = SubTopicId, sessionId = session.Id, culture = culture });
                    }
                    return RedirectToPage("/VoiceInterview", new { subTopicId = SubTopicId, sessionId = session.Id });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = _localizer["Failed to start interview. Please try again."];
                return Page();
            }
        }
    }
} 