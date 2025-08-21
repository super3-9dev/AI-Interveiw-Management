using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using InterviewBot.Services;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IAIAgentService _aiService;

        public List<Topic>? Topics { get; set; }
        public List<InterviewSession> RecentSessions { get; set; } = new();

        public IndexModel(AppDbContext db, IAIAgentService aiService)
        {
            _db = db;
            _aiService = aiService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                Topics = await _db.Topics
                    .Include(t => t.SubTopics)
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                RecentSessions = await _db.InterviewSessions
                    .Include(s => s.SubTopic)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                Topics = new List<Topic>();
                RecentSessions = new List<InterviewSession>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostTestOpenAIAsync()
        {
            try
            {
                var result = await _aiService.TestOpenAIAsync();
                return new JsonResult(new { success = true, result = result });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}
