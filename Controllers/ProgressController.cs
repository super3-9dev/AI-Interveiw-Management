using Microsoft.AspNetCore.Mvc;
using InterviewBot.Services;
using InterviewBot.Models;

namespace InterviewBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgressController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProgressController> _logger;

        public ProgressController(IProfileService profileService, ILogger<ProgressController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProgress(int id)
        {
            try
            {
                // Get user ID from session or authentication
                var userId = GetCurrentUserId();
                
                // For testing purposes, if no user ID is found, try to get the profile directly
                Profile? profile;
                if (userId == 0)
                {
                    // Try to get profile without user ID for testing
                    profile = await _profileService.GetProfileAsync(id, 1); // Use default user ID 1 for testing
                    if (profile == null)
                    {
                        return NotFound($"Profile {id} not found");
                    }
                }
                else
                {
                    profile = await _profileService.GetProfileAsync(id, userId);
                    if (profile == null)
                    {
                        return NotFound($"Profile {id} not found for user {userId}");
                    }
                }

                var progressData = new
                {
                    progress = profile.Progress,
                    status = profile.Status,
                    currentStepDescription = profile.CurrentStepDescription
                };

                _logger.LogInformation("Progress data for profile {ProfileId}: Progress={Progress}, Status={Status}", 
                    id, profile.Progress, profile.Status);

                return Ok(progressData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for profile {ProfileId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private int GetCurrentUserId()
        {
            // This is a simplified version - in a real app, you'd get this from authentication
            // For now, we'll try to get it from the session or return a default
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            
            // Fallback: try to get from query string for testing
            var queryUserId = HttpContext.Request.Query["userId"].FirstOrDefault();
            if (int.TryParse(queryUserId, out int queryId))
            {
                return queryId;
            }
            
            return 0;
        }
    }
}
