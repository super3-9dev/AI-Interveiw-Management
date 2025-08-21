using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages.Topics
{
    public class TopicViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CompletionPercentage { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public List<TopicViewModel> Topics { get; set; } = new();

        public IndexModel(AppDbContext db) => _db = db;

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var topicsFromDb = await _db.Topics
                .Where(t => t.UserId == userId)
                .Include(t => t.SubTopics)
                    .ThenInclude(st => st.InterviewSessions)
                .ToListAsync();

            Topics = topicsFromDb.Select(topic =>
            {
                int completedSubTopics = 0;
                if (topic.SubTopics.Any())
                {
                    completedSubTopics = topic.SubTopics.Count(st => st.InterviewSessions.Any(s => s.IsCompleted));
                }

                int completionPercentage = 0;
                if (topic.SubTopics.Any())
                {
                    completionPercentage = (int)Math.Round((double)completedSubTopics / topic.SubTopics.Count * 100);
                }

                return new TopicViewModel
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    CompletionPercentage = completionPercentage
                };
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var topic = await _db.Topics.FindAsync(id);
            if (topic != null)
            {
                _db.Topics.Remove(topic);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }

}
