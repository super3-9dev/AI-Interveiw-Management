using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages
{
    [Authorize]
    public class InterviewFormatModel : PageModel
    {
        private readonly IInterviewCatalogService _interviewCatalogService;
        private readonly AppDbContext _db;

        [BindProperty(SupportsGet = true)]
        public string InterviewId { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? TaskId { get; set; }

        [BindProperty]
        public string Culture { get; set; } = string.Empty;

        public string? TaskTitle { get; set; }
        public bool IsTaskBased { get; set; } = false;

        public InterviewFormatModel(IInterviewCatalogService interviewCatalogService, AppDbContext db)
        {
            _interviewCatalogService = interviewCatalogService;
            _db = db;
        }

        public async Task OnGetAsync()
        {
            // If TaskId is provided, load task information
            if (!string.IsNullOrEmpty(TaskId) && int.TryParse(TaskId, out int taskIdInt))
            {
                var task = await _db.Tasks
                    .Include(t => t.Group)
                    .FirstOrDefaultAsync(t => t.Id == taskIdInt);

                if (task != null)
                {
                    IsTaskBased = true;
                    TaskTitle = task.TaskName;
                    // Set InterviewId to a special format for task-based interviews
                    InterviewId = $"task-{taskIdInt}";
                }
            }
            // The InterviewId is automatically bound from the query string
        }

        public async Task<IActionResult> OnPostStartTextInterviewAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Handle task-based interviews
                if (!string.IsNullOrEmpty(TaskId) && int.TryParse(TaskId, out int taskIdInt))
                {
                    var currentCulture = !string.IsNullOrEmpty(Culture) ? Culture : 
                        (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");
                    
                    return RedirectToPage("/TextInterview", new { interviewId = $"task-{taskIdInt}", taskId = taskIdInt, culture = currentCulture });
                }

                // Handle regular interview catalog
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    // Update the interviewKind to "text" for text interview
                    await _interviewCatalogService.UpdateInterviewKindAsync(catalogId, "text");
                }
                // Redirect to text interview page
                var currentCulture2 = !string.IsNullOrEmpty(Culture) ? Culture : 
                    (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");

                return RedirectToPage("/TextInterview", new { interviewId = InterviewId, culture = currentCulture2 });
            }
            catch (Exception)
            {
                // Log error and redirect to dashboard
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }
                return RedirectToPage("/Dashboard", new { culture = currentCulture });
            }
        }

        public async Task<IActionResult> OnPostStartVoiceInterviewAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Handle task-based interviews
                if (!string.IsNullOrEmpty(TaskId) && int.TryParse(TaskId, out int taskIdInt))
                {
                    var currentCulture = !string.IsNullOrEmpty(Culture) ? Culture : 
                        (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");
                    
                    return RedirectToPage("/VoiceInterview", new { interviewId = $"task-{taskIdInt}", taskId = taskIdInt, culture = currentCulture });
                }

                // Handle regular interview catalog
                if (int.TryParse(InterviewId, out int catalogId))
                {
                    // Update the interviewKind to "voice" for voice interview
                    await _interviewCatalogService.UpdateInterviewKindAsync(catalogId, "voice");
                }

                // Redirect to voice interview page
                var currentCulture2 = !string.IsNullOrEmpty(Culture) ? Culture : 
                    (HttpContext.Request.Query["culture"].ToString() ?? HttpContext.Request.Cookies["culture"] ?? "en");

                return RedirectToPage("/VoiceInterview", new { interviewId = InterviewId, culture = currentCulture2 });
            }
            catch (Exception)
            {
                // Log error and redirect to dashboard
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }
                return RedirectToPage("/Dashboard", new { culture = currentCulture });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
