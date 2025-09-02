using Microsoft.AspNetCore.Mvc;
using InterviewBot.Services;
using InterviewBot.Models;
using System.Security.Claims;

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
                // Get user ID from authentication context
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var profile = await _profileService.GetProfileAsync(id, userId.Value);
                if (profile == null)
                {
                    return NotFound($"Profile {id} not found for user {userId.Value}");
                }

                var progressData = new
                {
                    progress = profile.Progress,
                    status = profile.Status
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
