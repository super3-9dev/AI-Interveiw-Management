using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    public class PublicInterviewModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public int SubTopicId { get; set; }

        public string? ErrorMessage { get; set; }
        public SubTopic? SubTopic { get; set; }

        public PublicInterviewModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync(int? subTopicId, int? id, string? action)
        {
            // Handle both route parameter (subTopicId) and query parameter (id)
            var topicId = subTopicId ?? id;
            
            if (!topicId.HasValue)
            {
                return RedirectToPage("/Error");
            }

            SubTopicId = topicId.Value;

            // Load the subtopic
            SubTopic = await _db.SubTopics
                .Include(st => st.Topic)
                .FirstOrDefaultAsync(st => st.Id == SubTopicId);

            if (SubTopic == null)
            {
                return NotFound();
            }

            // Handle createSession action (after email verification)
            if (action == "createSession")
            {
                var verifiedEmail = HttpContext.Session.GetString("VerifiedEmail");
                if (string.IsNullOrEmpty(verifiedEmail))
                {
                    return RedirectToPage("/Error");
                }

                try
                {
                    // Create a new interview session
                    var session = new InterviewSession
                    {
                        SubTopicId = SubTopicId,
                        CandidateEmail = verifiedEmail,
                        StartTime = DateTime.UtcNow,
                        Language = 0, // Default language
                        IsCompleted = false
                    };

                    _db.InterviewSessions.Add(session);
                    await _db.SaveChangesAsync();

                    // Store session info in session
                    HttpContext.Session.SetString("CurrentSessionId", session.Id.ToString());
                    HttpContext.Session.SetString("InterviewFormat", "text");
                    HttpContext.Session.SetString("PublicInterviewEmail", verifiedEmail);

                    // Clear verification data
                    HttpContext.Session.Remove("VerifiedEmail");
                    HttpContext.Session.Remove("IsEmailVerified");
                    HttpContext.Session.Remove("PublicInterviewEmail");
                    HttpContext.Session.Remove("PublicInterviewSubTopicId");

                    // Redirect to regular chat (not public chat)
                    return RedirectToPage("/Chat", new { subTopicId = SubTopicId, sessionId = session.Id });
                }
                catch (Exception)
                {
                    return RedirectToPage("/Error");
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return Page();
            }

            try
            {
                // Check if user has already completed this interview
                var existingSession = await _db.InterviewSessions
                    .Where(s => s.SubTopicId == SubTopicId && s.CandidateEmail == Email && s.IsCompleted)
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    ErrorMessage = "You have already completed this interview. You cannot access it again.";
                    return Page();
                }

                // Store email and subtopic info in session for email verification
                HttpContext.Session.SetString("PublicInterviewEmail", Email);
                HttpContext.Session.SetString("PublicInterviewSubTopicId", SubTopicId.ToString());

                // Redirect to email verification page
                return RedirectToPage("/EmailVerification", new { subTopicId = SubTopicId });
            }
            catch (Exception)
            {
                ErrorMessage = "Failed to start interview. Please try again.";
                return Page();
            }
        }
    }
} 