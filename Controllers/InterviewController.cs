using Microsoft.AspNetCore.Mvc;
using InterviewBot.Services;
using Microsoft.AspNetCore.Authorization;

namespace InterviewBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InterviewController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;

        public InterviewController(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        [HttpPost("generate-response")]
        public async Task<IActionResult> GenerateResponse([FromBody] GenerateResponseRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserMessage))
                {
                    return BadRequest("User message is required");
                }

                var response = await _openAIService.GenerateInterviewResponseAsync(
                    request.UserMessage,
                    request.InterviewContext ?? "Professional Career Interview"
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate response", message = ex.Message });
            }
        }

        [HttpPost("transcribe-audio")]
        public async Task<IActionResult> TranscribeAudio(IFormFile audioFile)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    return BadRequest("Audio file is required");
                }

                if (audioFile.Length > 25 * 1024 * 1024) // 25MB limit
                {
                    return BadRequest("Audio file size must be less than 25MB");
                }

                using var memoryStream = new MemoryStream();
                await audioFile.CopyToAsync(memoryStream);
                var audioData = memoryStream.ToArray();

                var transcription = await _openAIService.TranscribeAudioAsync(
                    audioData,
                    audioFile.FileName
                );

                return Ok(new { transcription });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to transcribe audio", message = ex.Message });
            }
        }

        [HttpPost("generate-speech")]
        public async Task<IActionResult> GenerateSpeech([FromBody] GenerateSpeechRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Text is required");
                }

                var audioData = await _openAIService.GenerateSpeechAsync(request.Text);

                if (audioData.Length == 0)
                {
                    return StatusCode(500, new { error = "Failed to generate speech" });
                }

                return File(audioData, "audio/mpeg", "speech.mp3");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate speech", message = ex.Message });
            }
        }

        [HttpPost("generate-follow-up")]
        public async Task<IActionResult> GenerateFollowUp([FromBody] GenerateFollowUpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserResponse) || string.IsNullOrEmpty(request.CurrentQuestion))
                {
                    return BadRequest("User response and current question are required");
                }

                var followUpQuestion = await _openAIService.GenerateFollowUpQuestionAsync(
                    request.UserResponse,
                    request.CurrentQuestion,
                    request.InterviewType ?? "Professional Career Interview"
                );

                return Ok(new { followUpQuestion });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate follow-up question", message = ex.Message });
            }
        }
    }

    public class GenerateResponseRequest
    {
        public string UserMessage { get; set; } = string.Empty;
        public string? InterviewContext { get; set; }
    }

    public class GenerateSpeechRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GenerateFollowUpRequest
    {
        public string UserResponse { get; set; } = string.Empty;
        public string CurrentQuestion { get; set; } = string.Empty;
        public string? InterviewType { get; set; }
    }
}
