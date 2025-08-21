using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InterviewBot.Pages.SubTopics
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty] public SubTopic SubTopic { get; set; } = new();

        public SelectList? TopicOptions { get; set; }

        public EditModel(AppDbContext db) => _db = db;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var subTopic = await _db.SubTopics.FindAsync(id);
            if (subTopic == null || subTopic.UserId != userId) return NotFound();

            SubTopic = subTopic;
            TopicOptions = new SelectList(_db.Topics.Where(t => t.UserId == userId).ToList(), "Id", "Title", subTopic.TopicId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"OnPostAsync called. ModelState.IsValid: {ModelState.IsValid}");
            
            // Clear validation errors for navigation properties that shouldn't be validated during POST
            ModelState.Remove("SubTopic.Topic");
            ModelState.Remove("SubTopic.User");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Validation errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"- {error.ErrorMessage}");
                }
                
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                TopicOptions = new SelectList(_db.Topics.Where(t => t.UserId == userId).ToList(), "Id", "Title");
                return Page();
            }

            var userIdCheck = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var subTopic = await _db.SubTopics.FindAsync(SubTopic.Id);
            if (subTopic == null || subTopic.UserId != userIdCheck) return NotFound();

            var topic = await _db.Topics.FindAsync(SubTopic.TopicId);
            if (topic == null || topic.UserId != userIdCheck)
            {
                ModelState.AddModelError("SubTopic.TopicId", "Selected topic doesn't exist");
                TopicOptions = new SelectList(_db.Topics.Where(t => t.UserId == userIdCheck).ToList(), "Id", "Title");
                return Page();
            }

            try
            {
                subTopic.Title = SubTopic.Title;
                subTopic.Description = SubTopic.Description;
                subTopic.CandidateEmail = SubTopic.CandidateEmail;
                subTopic.TopicId = SubTopic.TopicId;
                _db.SubTopics.Update(subTopic);
                await _db.SaveChangesAsync();
                
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error saving subtopic: " + ex.Message);
                TopicOptions = new SelectList(_db.Topics.Where(t => t.UserId == userIdCheck).ToList(), "Id", "Title");
                return Page();
            }
        }
    }
}