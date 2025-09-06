using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
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

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.7;
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIConfig _config;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _baseUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(IConfiguration config, ILogger<OpenAIService> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;

            // Load configuration
            _config = new OpenAIConfig
            {
                ApiKey = config["OpenAI:ApiKey"] ?? string.Empty,
                Model = config["OpenAI:Model"] ?? "gpt-4",
                MaxTokens = int.TryParse(config["OpenAI:MaxTokens"], out var maxTokens) ? maxTokens : 2000,
                Temperature = double.TryParse(config["OpenAI:Temperature"], out var temp) ? Math.Min(Math.Max(temp, 0.0), 2.0) : 0.7
            };
            
            // Debug logging for configuration
            _logger.LogInformation("OpenAI Configuration loaded - Model: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}", 
                _config.Model, _config.MaxTokens, _config.Temperature);

            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                _logger.LogError("OpenAI API key is not configured");
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _logger.LogInformation("OpenAI service initialized with model: {Model}", _config.Model);
        }

        public async Task<string> GenerateInterviewResponseAsync(string userMessage, string interviewContext)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Generating interview response (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                    var systemPrompt = GetInterviewSystemPrompt(interviewContext);
                    
                    // Debug logging for temperature
                    _logger.LogInformation("Using temperature: {Temperature}", _config.Temperature);
                    
                    // Ensure temperature is within valid range
                    var validTemperature = Math.Min(Math.Max(_config.Temperature, 0.0), 2.0);
                    _logger.LogInformation("Validated temperature: {Temperature}", validTemperature);
                    
                    var requestBody = new
                    {
                        model = _config.Model,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userMessage }
                        },
                        max_tokens = _config.MaxTokens,
                        temperature = validTemperature
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_baseUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("OpenAI API returned error: {StatusCode} - {ErrorContent}", response.StatusCode, responseContent);

                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in 2 seconds...");
                            await Task.Delay(2000);
                            continue;
                        }

                        return "I apologize, but I'm experiencing technical difficulties. Please try again.";
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, jsonOptions);

                    if (responseObject?.Choices == null || responseObject.Choices.Count == 0)
                    {
                        _logger.LogError("OpenAI API returned no choices in response");

                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in 2 seconds...");
                            await Task.Delay(2000);
                            continue;
                        }

                        return "I apologize, but I couldn't generate a response at this time.";
                    }

                    var firstChoice = responseObject.Choices.First();
                    if (firstChoice?.Message?.Content == null)
                    {
                        _logger.LogError("OpenAI API choice has no message content");

                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in 2 seconds...");
                            await Task.Delay(2000);
                            continue;
                        }

                        return "I apologize, but I couldn't generate a response at this time.";
                    }

                    var result = firstChoice.Message.Content.Trim();
                    _logger.LogInformation("Interview response generated successfully");

                    return result;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request error when calling OpenAI API (attempt {Attempt})", attempt);

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying in 2 seconds...");
                        await Task.Delay(2000);
                        continue;
                    }

                    return "I apologize, but I'm having trouble connecting to the AI service. Please check your internet connection and try again.";
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON parsing error when processing OpenAI response (attempt {Attempt})", attempt);

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying in 2 seconds...");
                        await Task.Delay(2000);
                        continue;
                    }

                    return "I apologize, but I received an invalid response from the AI service. Please try again.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in OpenAI service (attempt {Attempt})", attempt);

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Retrying in 2 seconds...");
                        await Task.Delay(2000);
                        continue;
                    }

                    return "I apologize, but I'm experiencing an unexpected error. Please try again later.";
                }
            }

            return "Failed to get response from OpenAI API after multiple attempts.";
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioData, string fileName)
        {
            try
            {
                _logger.LogInformation("Transcribing audio file: {FileName}", fileName);

                // Create multipart form data for audio transcription
                using var content = new MultipartFormDataContent();
                using var audioStream = new MemoryStream(audioData);
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "file", fileName);

                var modelContent = new StringContent("whisper-1");
                content.Add(modelContent, "model");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI Whisper API returned error: {StatusCode} - {ErrorContent}", response.StatusCode, responseContent);
                    return "Error transcribing audio. Please try again.";
                }

                // Parse the response to extract the transcription
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var text = responseData.GetProperty("text").GetString();

                _logger.LogInformation("Audio transcription completed successfully");
                return text?.Trim() ?? "Could not transcribe audio.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transcribing audio");
                return "Error transcribing audio. Please try again.";
            }
        }

        public async Task<byte[]> GenerateSpeechAsync(string text)
        {
            try
            {
                _logger.LogInformation("Generating speech for text length: {TextLength}", text.Length);

                var requestBody = new
                {
                    input = text,
                    model = "tts-1",
                    voice = "alloy",
                    response_format = "mp3",
                    speed = 1.0f
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/speech", httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI TTS API returned error: {StatusCode}", response.StatusCode);
                    return new byte[0];
                }

                var audioData = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Speech generation completed successfully. Audio size: {AudioSize} bytes", audioData.Length);

                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");
                return new byte[0];
            }
        }

        public async Task<string> GenerateFollowUpQuestionAsync(string userResponse, string currentQuestion, string interviewType)
        {
            try
            {
                _logger.LogInformation("Generating follow-up question for interview type: {InterviewType}", interviewType);

                var prompt = $@"Based on the user's response: ""{userResponse}""
                To the previous question: ""{currentQuestion}""
                In the context of a {interviewType} interview,
                Generate a relevant follow-up question that:
                1. Builds upon their answer
                2. Explores deeper aspects of their experience
                3. Maintains the professional interview flow
                4. Is specific and actionable

                Return only the question, no additional text.";

                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are an expert career interviewer. Generate relevant follow-up questions based on user responses." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 150,
                    temperature = 0.7f
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_baseUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API returned error for follow-up question: {StatusCode}", response.StatusCode);
                    return "Can you tell me more about that?";
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, jsonOptions);

                if (responseObject?.Choices == null || responseObject.Choices.Count == 0)
                {
                    _logger.LogError("OpenAI API returned no choices for follow-up question");
                    return "Can you tell me more about that?";
                }

                var firstChoice = responseObject.Choices.First();
                if (firstChoice?.Message?.Content == null)
                {
                    _logger.LogError("OpenAI API choice has no message content for follow-up question");
                    return "Can you tell me more about that?";
                }

                var result = firstChoice.Message.Content.Trim();
                _logger.LogInformation("Follow-up question generated successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating follow-up question");
                return "Can you tell me more about that?";
            }
        }

        private string GetInterviewSystemPrompt(string interviewContext)
        {
            return $@"You are an expert career coach conducting a {interviewContext} interview. 

IMPORTANT: Ask SHORT, DIRECT questions only. Each question should be 1-2 sentences maximum.

Good question examples:
- Why are you interested in consulting?
- What leadership experience do you have?
- What are your biggest weaknesses?
- Where do you see yourself in 5-7 years?
- Tell me about a challenging project you worked on.
- What motivates you in your career?

Your role:
1. Ask one short, direct question at a time
2. Keep questions conversational and professional
3. Focus on career, skills, experience, and goals
4. Do NOT provide long explanations or multiple questions
5. Do NOT give advice or feedback - just ask questions
6. Always end with a single question

Current interview context: {interviewContext}

Ask your first question now:";
        }

        // Response classes for OpenAI API
        private class OpenAIResponse
        {
            public string? Id { get; set; }
            public string? Object { get; set; }
            public long Created { get; set; }
            public string? Model { get; set; }
            public List<Choice>? Choices { get; set; }
            public Usage? Usage { get; set; }
        }

        private class Choice
        {
            public int Index { get; set; }
            public Message? Message { get; set; }
            public string? FinishReason { get; set; }
            public object? Logprobs { get; set; }
        }

        private class Message
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
            public object? Refusal { get; set; }
            public object? Annotations { get; set; }
        }

        private class Usage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
            public object? PromptTokensDetails { get; set; }
            public object? CompletionTokensDetails { get; set; }
        }
    }
}
