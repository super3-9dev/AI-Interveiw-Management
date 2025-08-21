using InterviewBot.Data;
using InterviewBot.Models;
using InterviewBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InterviewBot.Pages.SubTopics
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        [BindProperty]
        public SubTopicInputModel SubTopic { get; set; } = new();

        public SelectList? TopicOptions { get; set; }

        public CreateModel(AppDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        //public void OnGet()
        //{
        //    TopicOptions = new SelectList(_db.Topics.ToList(), "Id", "Title");
        //}


        public void OnGet(int? topicId)
        {
            // Get the current user's ID
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            
            // Only show topics that belong to the current user
            var userTopics = _db.Topics.Where(t => t.UserId == userId).ToList();
            TopicOptions = new SelectList(userTopics, "Id", "Title");

            // Preselect the topic if topicId is provided and it belongs to the user
            if (topicId.HasValue && userTopics.Any(t => t.Id == topicId.Value))
            {
                SubTopic.TopicId = topicId.Value;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TopicOptions = new SelectList(_db.Topics.ToList(), "Id", "Title");
                return Page();
            }

            try
            {
                // Get the current user's ID
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                
                // Verify the topic exists and belongs to the current user
                var topicExists = await _db.Topics.AnyAsync(t => t.Id == SubTopic.TopicId && t.UserId == userId);
                if (!topicExists)
                {
                    ModelState.AddModelError("SubTopic.TopicId", "Selected topic doesn't exist or doesn't belong to you");
                    // Reload user's topics for the dropdown
                    var userTopics = _db.Topics.Where(t => t.UserId == userId).ToList();
                    TopicOptions = new SelectList(userTopics, "Id", "Title");
                    return Page();
                }
                var newSubTopic = new SubTopic
                {
                    Title = SubTopic.Title,
                    Description = SubTopic.Description,
                    CandidateEmail = SubTopic.CandidateEmail,
                    TopicId = SubTopic.TopicId,
                    UserId = userId
                };

                _db.SubTopics.Add(newSubTopic);
                await _db.SaveChangesAsync();

                return RedirectToPage("Index", new { topicId = newSubTopic.TopicId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error saving subtopic: " + ex.Message);
                // Reload user's topics for the dropdown
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var userTopics = _db.Topics.Where(t => t.UserId == userId).ToList();
                TopicOptions = new SelectList(userTopics, "Id", "Title");
                return Page();
            }
        }

        public class SubTopicInputModel
        {
            [Required]
            [StringLength(100)]
            public string Title { get; set; } = null!;

            public string? Description { get; set; }

            [Required]
            [MultipleEmailAddress]
            public string CandidateEmail { get; set; } = null!;

            [Required]
            public int TopicId { get; set; }
        }
    }
}