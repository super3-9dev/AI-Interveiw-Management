using InterviewBot.Data;
using InterviewBot.Models;
using InterviewBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages.SubTopics
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;

        public List<SubTopicViewModel> SubTopics { get; set; } = new();
        public Topic? CurrentTopic { get; set; }

        public IndexModel(AppDbContext db, IEmailService emailService, ILogger<IndexModel> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int? topicId, string? culture)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (topicId == null)
            {
                // If no topic is specified, get all subtopics for the user
                var allSubTopics = await _db.SubTopics
                    .Where(st => st.UserId == userId)
                    .Include(st => st.Topic)
                    .Include(st => st.InterviewSessions)
                    .OrderBy(st => st.Topic.Title).ThenBy(st => st.Title)
                    .ToListAsync();
                
                SubTopics = allSubTopics.Select(st =>
                {
                    var latestSession = st.InterviewSessions.OrderByDescending(s => s.StartTime).FirstOrDefault();
                    return new SubTopicViewModel
                    {
                        Id = st.Id,
                        Title = st.Title,
                        TopicName = st.Topic.Title,
                        IsInterviewCompleted = latestSession?.IsCompleted ?? false,
                        CompletedSessionId = latestSession?.IsCompleted == true ? latestSession.Id : null,
                        LatestSessionId = latestSession?.Id,
                        HasIncompleteSession = latestSession != null && !latestSession.IsCompleted && (latestSession.EndTime.HasValue || latestSession.CurrentQuestionNumber > 0),
                        IsNewInterview = latestSession == null,
                        IsPublished = st.IsPublished
                    };
                }).ToList();

                // Set culture if provided
                if (!string.IsNullOrEmpty(culture))
                {
                    Response.Cookies.Append("culture", culture, new CookieOptions
                    {
                        Path = "/",
                        Expires = DateTime.UtcNow.AddYears(1)
                    });
                }

                return Page();
            }

            CurrentTopic = await _db.Topics.FindAsync(topicId);

            if (CurrentTopic == null || CurrentTopic.UserId != userId)
            {
                return NotFound();
            }

            var subTopicsFromDb = await _db.SubTopics
                .Where(st => st.TopicId == topicId && st.UserId == userId)
                .Include(st => st.InterviewSessions)
                .ToListAsync();

            SubTopics = subTopicsFromDb.Select(st =>
            {
                var latestSession = st.InterviewSessions.OrderByDescending(s => s.StartTime).FirstOrDefault();
                return new SubTopicViewModel
                {
                    Id = st.Id,
                    Title = st.Title,
                    TopicName = CurrentTopic.Title,
                    IsInterviewCompleted = latestSession?.IsCompleted ?? false,
                    CompletedSessionId = latestSession?.IsCompleted == true ? latestSession.Id : null,
                    LatestSessionId = latestSession?.Id,
                    HasIncompleteSession = latestSession != null && !latestSession.IsCompleted && (latestSession.EndTime.HasValue || latestSession.CurrentQuestionNumber > 0),
                    IsNewInterview = latestSession == null,
                    IsPublished = st.IsPublished
                };
            }).ToList();

            // Set culture if provided
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append("culture", culture, new CookieOptions
                {
                    Path = "/",
                    Expires = DateTime.UtcNow.AddYears(1)
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int? topicId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subTopic = await _db.SubTopics.FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

            if (subTopic != null)
            {
                _db.SubTopics.Remove(subTopic);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { topicId });
        }

        public async Task<IActionResult> OnPostPublishAsync(int id, int? topicId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subTopic = await _db.SubTopics
                .Include(st => st.Topic)
                .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);
            if (subTopic != null && !subTopic.IsPublished)
            {
                // Only allow publishing if not already published
                subTopic.IsPublished = true;
                
                await _db.SaveChangesAsync();

                // Get culture from query string or cookie
                var culture = Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(culture))
                {
                    culture = Request.Cookies["culture"];
                }

                // Send email when publishing (optional - won't block publish if email fails)
                try
                {
                    await _emailService.SendInterviewInviteAsync(subTopic, culture);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the publish operation
                    // Email sending is optional - the publish operation still succeeds
                    _logger.LogWarning(ex, "Failed to send email for subtopic {SubTopicId}, but publish operation completed successfully", subTopic.Id);
                }
            }

            return RedirectToPage(new { topicId });
        }

        public async Task<IActionResult> OnPostRepublishAsync(int id, int? topicId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subTopic = await _db.SubTopics
                .Include(st => st.Topic)
                .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);
            if (subTopic != null && subTopic.IsPublished)
            {
                // Get culture from query string or cookie
                var culture = Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(culture))
                {
                    culture = Request.Cookies["culture"];
                }

                // Republish - send email again for already published subtopics
                try
                {
                    await _emailService.SendInterviewInviteAsync(subTopic, culture);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the republish operation
                    _logger.LogWarning(ex, "Failed to send email for subtopic {SubTopicId} during republish", subTopic.Id);
                }
            }

            return RedirectToPage(new { topicId });
        }
    }

    public class SubTopicViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public bool IsInterviewCompleted { get; set; }
        public int? CompletedSessionId { get; set; }
        public int? LatestSessionId { get; set; }
        public bool HasIncompleteSession { get; set; }
        public bool IsNewInterview { get; set; }
        public bool IsPublished { get; set; }
    }
}