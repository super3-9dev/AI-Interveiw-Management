using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Pages
{
    [Authorize]
    public class GroupManageModel : PageModel
    {
        private readonly AppDbContext _db;

        public GroupManageModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string StudentEmails { get; set; } = string.Empty;

        [BindProperty]
        public int? TaskId { get; set; }

        [BindProperty]
        public string TaskAgent { get; set; } = string.Empty;

        [BindProperty]
        public string TaskName { get; set; } = string.Empty;

        [BindProperty]
        public string Instructions { get; set; } = string.Empty;

        [BindProperty]
        public string Objective { get; set; } = string.Empty;

        [BindProperty]
        public string Constraints { get; set; } = string.Empty;

        [BindProperty]
        public string Emphasis { get; set; } = string.Empty;

        [BindProperty]
        public bool TaskIsVisible { get; set; } = false;

        [BindProperty]
        public int? ResourceId { get; set; }

        [BindProperty]
        public string ResourceTitle { get; set; } = string.Empty;

        [BindProperty]
        public string ResourceUrl { get; set; } = string.Empty;

        public Group? Group { get; set; }
        public List<GroupTask> Tasks { get; set; } = new();
        public List<GroupResource> Resources { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string ActiveTab { get; set; } = "students";

        public async Task<IActionResult> OnGetAsync(string tab = "students")
        {
            // Check if user is a Teacher (idProfile = 1)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 1)
            {
                // Redirect non-teachers to home page
                return RedirectToPage("/Index");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Load the group and verify ownership
            Group = await _db.Groups
                .FirstOrDefaultAsync(g => g.Id == Id && g.UserId == userId.Value);

            if (Group == null)
            {
                ErrorMessage = "Group not found or you don't have permission to access it.";
                return RedirectToPage("/MyGroups");
            }

            // Set active tab
            ActiveTab = tab?.ToLower() ?? "students";

            // Load existing emails into StudentEmails property for display
            if (Group != null && !string.IsNullOrWhiteSpace(Group.Emails))
            {
                // Format emails for display (comma-separated)
                StudentEmails = Group.Emails;
            }

            // Load tasks if on tasks tab
            if (ActiveTab == "tasks")
            {
                await LoadTasksAsync();
            }

            // Load resources if on resources tab (using mock data for now)
            if (ActiveTab == "resources")
            {
                LoadMockResources();
            }

            return Page();
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

        public async Task<IActionResult> OnPostAddStudentsAsync()
        {
            // Check if user is a Teacher (idProfile = 1)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 1)
            {
                return RedirectToPage("/Index");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Load the group and verify ownership
            Group = await _db.Groups
                .FirstOrDefaultAsync(g => g.Id == Id && g.UserId == userId.Value);

            if (Group == null)
            {
                ErrorMessage = "Group not found or you don't have permission to access it.";
                ActiveTab = "students";
                return Page();
            }

            ActiveTab = "students";

            // Validate input
            if (string.IsNullOrWhiteSpace(StudentEmails))
            {
                ErrorMessage = "Please enter at least one student email.";
                return Page();
            }

            // Parse emails - support both comma and newline separators
            var emails = new List<string>();
            
            // First, try to split by comma
            if (StudentEmails.Contains(','))
            {
                emails = StudentEmails
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();
            }
            else
            {
                // If no comma, split by newlines
                emails = StudentEmails
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();
            }

            if (emails.Count == 0)
            {
                ErrorMessage = "Please enter at least one valid email address.";
                return Page();
            }

            // Validate email format
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            var invalidEmails = emails.Where(e => !emailRegex.IsMatch(e)).ToList();

            if (invalidEmails.Any())
            {
                ErrorMessage = $"Invalid email format(s): {string.Join(", ", invalidEmails.Take(5))}";
                return Page();
            }

            try
            {
                // Replace all emails with the new ones (user can edit existing emails)
                // Store emails in the database (separated by commas)
                Group.Emails = string.Join(",", emails);
                
                // Update student count
                Group.StudentCount = emails.Count;
                Group.UpdatedAt = DateTime.UtcNow;

                // Save to database
                await _db.SaveChangesAsync();

                SuccessMessage = $"Successfully updated {emails.Count} student(s) in the group.";
                // Keep the emails in the textarea for further editing
                StudentEmails = Group.Emails;

                // Redirect to refresh the page
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }

                return RedirectToPage("/GroupManage", new { id = Id, tab = "students", culture = currentCulture });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding students: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateTaskAsync()
        {
            // Check if user is a Teacher (idProfile = 1)
            var idProfileClaim = User.FindFirst("IdProfile");
            if (idProfileClaim == null || !int.TryParse(idProfileClaim.Value, out int idProfile) || idProfile != 1)
            {
                return RedirectToPage("/Index");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Load the group and verify ownership
            Group = await _db.Groups
                .FirstOrDefaultAsync(g => g.Id == Id && g.UserId == userId.Value);

            if (Group == null)
            {
                ErrorMessage = "Group not found or you don't have permission to access it.";
                ActiveTab = "tasks";
                await LoadTasksAsync();
                return Page();
            }

            ActiveTab = "tasks";

            // Validate input - AgentName and TaskName are required
            if (string.IsNullOrWhiteSpace(TaskAgent))
            {
                ErrorMessage = "Please enter an agent name.";
                await LoadTasksAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(TaskName))
            {
                ErrorMessage = "Please enter a task name.";
                await LoadTasksAsync();
                return Page();
            }

            // Validate field lengths
            if (TaskAgent.Length > 255)
            {
                ErrorMessage = "Agent name cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            if (TaskName.Length > 255)
            {
                ErrorMessage = "Task name cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Instructions) && Instructions.Length > 255)
            {
                ErrorMessage = "Instructions cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Objective) && Objective.Length > 255)
            {
                ErrorMessage = "Objective cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Constraints) && Constraints.Length > 255)
            {
                ErrorMessage = "Constraints cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Emphasis) && Emphasis.Length > 255)
            {
                ErrorMessage = "Emphasis cannot exceed 255 characters.";
                await LoadTasksAsync();
                return Page();
            }

            try
            {
                GroupTask taskToSave;
                
                if (TaskId.HasValue && TaskId.Value > 0)
                {
                    // Edit existing task
                    taskToSave = await _db.Tasks
                        .FirstOrDefaultAsync(t => t.Id == TaskId.Value && t.GroupId == Group.Id);
                    
                    if (taskToSave == null)
                    {
                        ErrorMessage = "Task not found or you don't have permission to edit it.";
                        await LoadTasksAsync();
                        return Page();
                    }
                    
                    // Update task properties
                    taskToSave.AgentName = TaskAgent.Trim();
                    taskToSave.TaskName = TaskName.Trim();
                    taskToSave.Instructions = string.IsNullOrWhiteSpace(Instructions) ? null : Instructions.Trim();
                    taskToSave.Objective = string.IsNullOrWhiteSpace(Objective) ? null : Objective.Trim();
                    taskToSave.Constraints = string.IsNullOrWhiteSpace(Constraints) ? null : Constraints.Trim();
                    taskToSave.Emphasis = string.IsNullOrWhiteSpace(Emphasis) ? null : Emphasis.Trim();
                    taskToSave.IsVisible = TaskIsVisible;
                    taskToSave.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new task
                    taskToSave = new GroupTask
                    {
                        AgentName = TaskAgent.Trim(),
                        TaskName = TaskName.Trim(),
                        Instructions = string.IsNullOrWhiteSpace(Instructions) ? null : Instructions.Trim(),
                        Objective = string.IsNullOrWhiteSpace(Objective) ? null : Objective.Trim(),
                        Constraints = string.IsNullOrWhiteSpace(Constraints) ? null : Constraints.Trim(),
                        Emphasis = string.IsNullOrWhiteSpace(Emphasis) ? null : Emphasis.Trim(),
                        GroupId = Group.Id,
                        IsVisible = TaskIsVisible,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _db.Tasks.Add(taskToSave);
                }

                // Save to database
                await _db.SaveChangesAsync();
                
                // Reload tasks from database
                await LoadTasksAsync();

                SuccessMessage = TaskId.HasValue ? "Task updated successfully!" : "Task created successfully!";
                // Clear form
                TaskId = null;
                TaskAgent = string.Empty;
                TaskName = string.Empty;
                Instructions = string.Empty;
                Objective = string.Empty;
                Constraints = string.Empty;
                Emphasis = string.Empty;
                TaskIsVisible = false;

                // Redirect to refresh the page
                var currentCulture = HttpContext.Request.Query["culture"].ToString();
                if (string.IsNullOrEmpty(currentCulture))
                {
                    currentCulture = HttpContext.Request.Cookies["culture"] ?? "en";
                }

                return RedirectToPage("/GroupManage", new { id = Id, tab = "tasks", culture = currentCulture });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error saving task: " + ex.Message;
                await LoadTasksAsync();
                return Page();
            }
        }

        private async Task LoadTasksAsync()
        {
            if (Group == null)
            {
                Tasks = new List<GroupTask>();
                return;
            }

            try
            {
                Tasks = await _db.Tasks
                    .Where(t => t.GroupId == Group.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .AsNoTracking() // Prevent tracking and loading navigation properties
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                ErrorMessage = "Error loading tasks: " + ex.Message;
                Tasks = new List<GroupTask>();
            }
        }

        private void LoadMockResources()
        {
            // Mock data for resources
            Resources = new List<GroupResource>
            {
                new GroupResource
                {
                    Id = 1,
                    Title = "Documentación de Agentes IA",
                    Url = "https://docs.example.com/agents",
                    GroupId = Group?.Id ?? 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new GroupResource
                {
                    Id = 2,
                    Title = "Tutorial de Tailwind CSS",
                    Url = "https://tailwindcss.com/docs",
                    GroupId = Group?.Id ?? 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };
        }

        private async Task LoadResourcesAsync()
        {
            if (Group == null) return;

            try
            {
                Resources = await _db.Resources
                    .Where(r => r.GroupId == Group.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch
            {
                // If table doesn't exist, use mock data
                LoadMockResources();
            }
        }
    }
}

