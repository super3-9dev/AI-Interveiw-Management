using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    public class VoiceInterviewModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IStringLocalizer<VoiceInterviewModel> _localizer;

        public VoiceInterviewModel(AppDbContext db, IStringLocalizer<VoiceInterviewModel> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        [BindProperty(SupportsGet = true)]
        public int SubTopicId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SessionId { get; set; }

        public SubTopic? SubTopic { get; set; }

        public async Task<IActionResult> OnGetAsync(string? culture)
        {
            try
            {
                var subTopic = await _db.SubTopics
                    .Include(st => st.Topic)
                    .FirstOrDefaultAsync(st => st.Id == SubTopicId);

                if (subTopic == null)
                {
                    return NotFound();
                }

                SubTopic = subTopic;

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading subtopic: {ex}");
                return RedirectToPage("/Error");
            }
        }
    }
} 