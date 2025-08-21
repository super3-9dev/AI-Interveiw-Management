using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    public class PublicChatModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty(SupportsGet = true)]
        public int SubTopicId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        public SubTopic SubTopic { get; set; } = null!;
        public InterviewSession Session { get; set; } = null!;

        public PublicChatModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load the subtopic
                var subTopic = await _db.SubTopics
                    .Include(st => st.Topic)
                    .FirstOrDefaultAsync(st => st.Id == SubTopicId);

                if (subTopic == null)
                {
                    return NotFound();
                }

                // Load the session
                var session = await _db.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == SessionId);

                if (session == null)
                {
                    return NotFound();
                }

                // Verify session belongs to this subtopic
                if (session.SubTopicId != SubTopicId)
                {
                    return BadRequest();
                }

                // Check if session is completed
                if (session.IsCompleted)
                {
                    return RedirectToPage("/PublicResults", new { id = SessionId });
                }

                SubTopic = subTopic;
                Session = session;

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading public chat: {ex}");
                return RedirectToPage("/Error");
            }
        }
    }
} 