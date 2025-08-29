using OpenAI;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace InterviewBot.Services
{
    public interface IOpenAIService
    {
        Task<string> GenerateInterviewResponseAsync(string userMessage, string interviewContext);
        Task<string> TranscribeAudioAsync(byte[] audioData, string fileName);
        Task<byte[]> GenerateSpeechAsync(string text);
        Task<string> GenerateFollowUpQuestionAsync(string userResponse, string currentQuestion, string interviewType);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly float _temperature;

        public OpenAIService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            var apiKey = _configuration["OpenAI:ApiKey"];
            _model = _configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
            _maxTokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "2000");
            _temperature = float.Parse(_configuration["OpenAI:Temperature"] ?? "0.7");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("OpenAI API key is not configured");
            }

            // Configure HttpClient for OpenAI API
            _httpClient.BaseAddress = new Uri("https://api.openai.com");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> GenerateInterviewResponseAsync(string userMessage, string interviewContext)
        {
            try
            {
                var messages = new List<object>
                {
                    new { role = "system", content = GetInterviewSystemPrompt(interviewContext) },
                    new { role = "user", content = userMessage }
                };

                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = _maxTokens,
                    temperature = _temperature
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/v1/chat/completions", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse the response to extract the AI message
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    var message = firstChoice.GetProperty("message");
                    var content = message.GetProperty("content").GetString();
                    return content?.Trim() ?? "I apologize, but I couldn't generate a response at this time.";
                }

                return "I apologize, but I couldn't generate a response at this time.";
            }
            catch (Exception ex)
            {
                // Log the error in production
                Console.WriteLine($"Error generating interview response: {ex.Message}");
                return "I apologize, but I'm experiencing technical difficulties. Please try again.";
            }
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioData, string fileName)
        {
            try
            {
                // Create multipart form data for audio transcription
                using var content = new MultipartFormDataContent();
                using var audioStream = new MemoryStream(audioData);
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "file", fileName);

                var modelContent = new StringContent("whisper-1");
                content.Add(modelContent, "model");

                var response = await _httpClient.PostAsync("/v1/audio/transcriptions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse the response to extract the transcription
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var text = responseData.GetProperty("text").GetString();
                return text?.Trim() ?? "Could not transcribe audio.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transcribing audio: {ex.Message}");
                return "Error transcribing audio. Please try again.";
            }
        }

        public async Task<byte[]> GenerateSpeechAsync(string text)
        {
            try
            {
                var requestBody = new
                {
                    input = text,
                    model = "tts-1",
                    voice = "alloy",
                    response_format = "mp3",
                    speed = 1.0f
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/v1/audio/speech", httpContent);
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating speech: {ex.Message}");
                return new byte[0];
            }
        }

        public async Task<string> GenerateFollowUpQuestionAsync(string userResponse, string currentQuestion, string interviewType)
        {
            try
            {
                var prompt = $@"Based on the user's response: ""{userResponse}""
                To the previous question: ""{currentQuestion}""
                In the context of a {interviewType} interview,
                Generate a relevant follow-up question that:
                1. Builds upon their answer
                2. Explores deeper aspects of their experience
                3. Maintains the professional interview flow
                4. Is specific and actionable

                Return only the question, no additional text.";

                var messages = new List<object>
                {
                    new { role = "system", content = "You are an expert career interviewer. Generate relevant follow-up questions based on user responses." },
                    new { role = "user", content = prompt }
                };

                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = 150,
                    temperature = 0.7f
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/v1/chat/completions", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse the response to extract the AI message
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var choices = responseData.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    var message = firstChoice.GetProperty("message");
                    var content = message.GetProperty("content").GetString();
                    return content?.Trim() ?? "Can you tell me more about that?";
                }

                return "Can you tell me more about that?";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating follow-up question: {ex.Message}");
                return "Can you tell me more about that?";
            }
        }

        private string GetInterviewSystemPrompt(string interviewContext)
        {
            return $@"You are an expert career coach conducting a {interviewContext} interview. Your role is to:

1. Ask relevant, professional questions based on the user's responses
2. Provide constructive feedback and encouragement
3. Guide the conversation naturally through different aspects of their career
4. Maintain a professional yet supportive tone
5. Ask follow-up questions that explore deeper insights
6. Help the user reflect on their experiences and skills

Current interview context: {interviewContext}

Keep your responses concise (1-2 sentences) and focused on asking the next question or providing brief feedback. Always end with a question to continue the conversation.";
        }
    }
}
