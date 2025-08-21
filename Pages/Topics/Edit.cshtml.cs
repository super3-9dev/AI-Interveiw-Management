using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewBot.Pages.Topics
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        [BindProperty] public Topic Topic { get; set; } = new();

        public EditModel(AppDbContext db) => _db = db;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var topic = await _db.Topics.FindAsync(id);
            if (topic == null || topic.UserId != userId) return NotFound();

            Topic = topic;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var topic = await _db.Topics.FindAsync(Topic.Id);
            if (topic == null || topic.UserId != userId) return NotFound();

            topic.Title = Topic.Title;
            topic.Objectives = Topic.Objectives;
            _db.Topics.Update(topic);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}