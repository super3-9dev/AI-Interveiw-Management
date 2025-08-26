//C:\Users\DELL\source\repos\InterviewBot\InterviewBot\Pages\InterviewSessions\Results.cshtml.cs
// Pages/InterviewSessions/Results.cshtml.cs
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages.InterviewSessions
{
    public class ResultsModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IStringLocalizer<ResultsModel> _localizer;

        [BindProperty]
        public InterviewSession Session { get; set; } = null!;

        public ResultsModel(AppDbContext db, IStringLocalizer<ResultsModel> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<IActionResult> OnGetAsync(int id, string? culture)
        {
            // Try to get user ID from claims (if authenticated)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int? userId = null;
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            // Query for the session - if user is authenticated, check ownership
            // If not authenticated, just get the session by ID
            var query = _db.InterviewSessions
                .Include(s => s.SubTopic)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.Result)
                    .ThenInclude(r => r!.Questions)
                .AsQueryable();

            if (userId.HasValue)
            {
                // Authenticated user - check ownership
                Session = await query.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);
            }
            else
            {
                // Anonymous user - just get by ID (for public results)
                Session = await query.FirstOrDefaultAsync(s => s.Id == id);
            }

            if (Session == null)
            {
                return NotFound();
            }

            if (!Session.IsCompleted)
            {
                // If user is authenticated, redirect to their sessions
                if (userId.HasValue)
                {
                    return RedirectToPage("/InterviewSessions/Index");
                }
                else
                {
                    // Anonymous user - show a message that results aren't ready
                    return RedirectToPage("/Error", new { message = _localizer["Interview results are not yet available."] });
                }
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
    }
}
