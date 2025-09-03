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

        public List<InterviewResult> InterviewResults { get; set; } = new();

        public ResultsModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                // Load interview results
                InterviewResults = await _db.InterviewResults
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CompleteDate)
                    .ToListAsync();
            }

            return Page();
        }
    }
}

