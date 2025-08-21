using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    public class PublicResultsModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty]
        public InterviewSession Session { get; set; } = null!;

        public PublicResultsModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var session = await _db.InterviewSessions
                .Include(s => s.SubTopic)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.Result)
                    .ThenInclude(r => r!.Questions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            if (!session.IsCompleted)
            {
                return RedirectToPage("/PublicInterview", new { subTopicId = session.SubTopicId });
            }

            Session = session;
            return Page();
        }
    }
} 