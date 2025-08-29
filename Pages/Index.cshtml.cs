using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public List<Profile> RecentProfiles { get; set; } = new();
        public List<InterviewSession> RecentSessions { get; set; } = new();

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                RecentProfiles = await _db.Profiles
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                RecentSessions = await _db.InterviewSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .Take(5)
                    .ToListAsync();
            }

            return Page();
        }
    }
}
