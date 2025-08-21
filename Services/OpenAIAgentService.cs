using System.Text;
using System.Text.Json;
using InterviewBot.Models;

namespace InterviewBot.Services
{
    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.7;
    }

    public class OpenAIAgentService : IAIAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIConfig _config;
        private readonly ILogger<OpenAIAgentService> _logger;
        private readonly string _baseUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIAgentService(IConfiguration config, ILogger<OpenAIAgentService> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            
            // Load configuration
            _config = new OpenAIConfig
            {
                ApiKey = config["OpenAI:ApiKey"] ?? string.Empty,
                Model = config["OpenAI:Model"] ?? "gpt-4",
                MaxTokens = int.TryParse(config["OpenAI:MaxTokens"], out var maxTokens) ? maxTokens : 2000,
                Temperature = 0.7
            };

            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                _logger.LogError("OpenAI API key is not configured");
                throw new InvalidOperationException("OpenAI API key is not configured");
            }

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _logger.LogInformation("OpenAI service initialized with model: {Model}", _config.Model);
        }

        public async Task<string> AskQuestionAsync(string message)
        {
            const int maxRetries = 3;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Sending request to OpenAI API (attempt {Attempt}/{MaxRetries}) with message length: {MessageLength}", 
                        attempt, maxRetries, message.Length);
                    _logger.LogInformation("Message preview: {MessagePreview}", message.Length > 100 ? message.Substring(0, 100) + "..." : message);
                    
                    var requestBody = new
                    {
                        model = _config.Model,
                        messages = new[]
                        {
                            new
                            {
                                role = "system",
                                content = "You are an expert technical interviewer. Provide clear, concise, and helpful responses. When generating questions, format them as a numbered list. When evaluating interviews, provide a score out of 100 and detailed feedback."
                            },
                            new
                            {
                                role = "user",
                                content = message
                            }
                        },
                        max_tokens = _config.MaxTokens,
                        temperature = _config.Temperature
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Sending request to OpenAI API: {Url}", _baseUrl);
                    var response = await _httpClient.PostAsync(_baseUrl, content);
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("OpenAI API response status: {StatusCode}", response.StatusCode);
                    _logger.LogInformation("OpenAI API response content length: {ResponseLength}", responseContent.Length);
                    _logger.LogInformation("OpenAI API response content: {ResponseContent}", responseContent);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("OpenAI API returned error: {StatusCode} - {ErrorContent}", response.StatusCode, responseContent);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in 2 seconds...");
                            await Task.Delay(2000);
                            continue;
                        }
                        
                        return $"OpenAI API Error: {response.StatusCode} - {responseContent}";
                    }

                    // Use proper JSON options for deserialization
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    _logger.LogInformation("Attempting to deserialize OpenAI response with options: {Options}", 
                        JsonSerializer.Serialize(jsonOptions, new JsonSerializerOptions { WriteIndented = true }));

                    var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, jsonOptions);
                    
                    _logger.LogInformation("Deserialized response object: {ResponseObject}", 
                        JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true }));
                    
                    if (responseObject?.Choices == null || responseObject.Choices.Count == 0)
                    {
                        _logger.LogError("OpenAI API returned no choices in response. Response object: {ResponseObject}", 
                            JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true }));
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in 2 seconds...");
                            await Task.Delay(2000);
                            continue;
                        }
                        
                        return "No response received from OpenAI API";
                    }

                    _logger.LogInformation("Found {ChoiceCount} choices in response", responseObject.Choices.Count);
                    
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
                        
                        return "No message content in OpenAI API response";
                    }

                    var result = firstChoice.Message.Content.Trim();
                    _logger.LogInformation("OpenAI API response received successfully. Result length: {ResultLength}", result.Length);
                    _logger.LogInformation("Result preview: {ResultPreview}", result.Length > 100 ? result.Substring(0, 100) + "..." : result);
                    
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

        public async Task<string> TestOpenAIAsync()
        {
            try
            {
                var testMessage = "Generate 3 simple programming questions about C#. Format as: 1. Question 2. Question 3. Question";
                _logger.LogInformation("Testing OpenAI API with simple message");
                
                var result = await AskQuestionAsync(testMessage);
                _logger.LogInformation("OpenAI test result: {Result}", result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing OpenAI API");
                return $"Test failed: {ex.Message}";
            }
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
