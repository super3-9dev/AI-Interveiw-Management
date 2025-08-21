using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages.InterviewSessions
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public List<InterviewSession> Sessions { get; set; } = new();

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task OnGetAsync()
        {
            // Try to get user ID from claims (if authenticated)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int? userId = null;
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            if (userId.HasValue)
            {
                // Authenticated user - get their sessions
                Sessions = await _db.InterviewSessions
                    .Include(s => s.SubTopic)
                        .ThenInclude(st => st.Topic)
                    .Include(s => s.Result)
                    .Where(s => s.UserId == userId.Value)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync();
            }
            else
            {
                // Anonymous user - show empty list or redirect to login
                Sessions = new List<InterviewSession>();
            }
        }
    }
}