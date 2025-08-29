using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class ResultsModel : PageModel
    {
        private readonly AppDbContext _db;

        public List<Profile> Profiles { get; set; } = new();
        public List<InterviewSession> InterviewSessions { get; set; } = new();

        public ResultsModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                Profiles = await _db.Profiles
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                InterviewSessions = await _db.InterviewSessions
                    .Include(s => s.InterviewCatalog)
                    .Include(s => s.CustomInterview)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .Take(10)
                    .ToListAsync();
            }

            return Page();
        }
    }
}

