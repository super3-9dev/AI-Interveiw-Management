using Microsoft.AspNetCore.SignalR;
using InterviewBot.Models;
using InterviewBot.Data;
using InterviewBot.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class InterviewQuestionTracker
{
    public List<string> AskedQuestions { get; } = new();
    public List<string> AvailableQuestions { get; } = new();

    public void MarkQuestionAsked(string question)
    {
        AskedQuestions.Add(question);
        AvailableQuestions.Remove(question);
    }
}

public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly IAIAgentService _aiService;
    private static readonly ConcurrentDictionary<string, InterviewSession> _sessions = new();
    private static readonly ConcurrentDictionary<string, InterviewQuestionTracker> _questionTrackers = new();
    private static readonly object _trackerLock = new object();
    private static readonly ConcurrentDictionary<string, int> _consecutiveNonAnswers = new();
    private static readonly ConcurrentDictionary<string, bool> _exitOfferPending = new();

    public ChatHub(AppDbContext db, IAIAgentService aiService)
    {
        _db = db;
        _aiService = aiService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_sessions.TryRemove(Context.ConnectionId, out var session))
        {
            try
            {
                session.EndTime = DateTime.UtcNow;
                if (!session.IsCompleted)
                {
                    session.IsCompleted = true;
                    session.Summary = "Disconnected before completion";
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving disconnected session: {ex}");
            }
        }
        _questionTrackers.TryRemove(Context.ConnectionId, out _);
        _consecutiveNonAnswers.TryRemove(Context.ConnectionId, out _);
        _exitOfferPending.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartInterview(int subTopicId, string language = "en")
    {
        try
        {
            if (_sessions.ContainsKey(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Interview already in progress");
                return;
            }

            var subTopic = await _db.SubTopics.Include(st => st.Topic).FirstOrDefaultAsync(st => st.Id == subTopicId);
            if (subTopic == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid subtopic selected");
                return;
            }

            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "User not authenticated");
                return;
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "User not found");
                return;
            }

            // Set the language for the interview session
            var interviewLanguage = language == "es" ? InterviewLanguage.Spanish : InterviewLanguage.English;

            var session = new InterviewSession
            {
                SubTopicId = subTopicId,
                StartTime = DateTime.UtcNow,
                Summary = string.Empty,
                Messages = new List<ChatMessage>(),
                CurrentQuestionNumber = 0,
                IsCompleted = false,
                UserId = userId,
                CandidateName = user.FullName,
                CandidateEmail = user.Email,
                CandidateEducation = user.Education ?? "Not specified",
                CandidateExperience = user.Experience ?? "0",
                SubTopic = subTopic,
                Language = interviewLanguage
            };

            _db.InterviewSessions.Add(session);
            await _db.SaveChangesAsync();

            _sessions.TryAdd(Context.ConnectionId, session);
            
            try
            {
                await InitializeQuestionTracker(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize question tracker: {ex}");
                // Clean up the session since we can't generate questions
                _sessions.TryRemove(Context.ConnectionId, out _);
                _db.InterviewSessions.Remove(session);
                await _db.SaveChangesAsync();
                
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to generate interview questions. Please try again later.");
                return;
            }

            // Send welcome message in the appropriate language
            var welcomeMessage = interviewLanguage == InterviewLanguage.Spanish 
                ? "¡Hola! Bienvenido a tu entrevista simulada. Di hola para comenzar."
                : "Hello! Welcome to your mock interview. Say hello to begin.";

            await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", welcomeMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in StartInterview: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to start interview. Please try again.");
        }
    }

    public async Task StartPublicInterview(int subTopicId, int sessionId)
    {
        try
        {
            if (_sessions.ContainsKey(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Interview already in progress");
                return;
            }

            var subTopic = await _db.SubTopics.Include(st => st.Topic).FirstOrDefaultAsync(st => st.Id == subTopicId);
            if (subTopic == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid subtopic selected");
                return;
            }

            var session = await _db.InterviewSessions
                .Include(s => s.SubTopic)
                .ThenInclude(st => st.Topic)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.SubTopicId == subTopicId);

            if (session == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid session");
                return;
            }

            if (session.IsCompleted)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "This interview session has already been completed");
                return;
            }

            // Store session in memory
            _sessions[Context.ConnectionId] = session;

            // Initialize question tracker
            await InitializeQuestionTracker(session);

            // Reset consecutive non-answers counter
            _consecutiveNonAnswers[Context.ConnectionId] = 0;
            _exitOfferPending[Context.ConnectionId] = false;

            // Send welcome message
            var welcomeMessage = session.Language == InterviewLanguage.Spanish 
                ? "¡Hola! Bienvenido a tu entrevista simulada. Di hola para comenzar."
                : "Hello! Welcome to your mock interview. Say hello to begin.";

            await Clients.Caller.SendAsync("ReceiveMessage", "AI Interviewer", welcomeMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in StartPublicInterview: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to start interview. Please try again.");
        }
    }

    public async Task SendPublicMessage(string message, int sessionId)
    {
        try
        {
            if (!_sessions.TryGetValue(Context.ConnectionId, out var session))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "No active interview session");
                return;
            }

            if (session.Id != sessionId)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid session");
                return;
            }

            // Process the message as an answer
            await SendAnswer(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendPublicMessage: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to process message. Please try again.");
        }
    }

    private async Task InitializeQuestionTracker(InterviewSession session)
    {
        lock (_trackerLock)
        {
            // Check if tracker already exists for this connection
            if (_questionTrackers.ContainsKey(Context.ConnectionId))
            {
                Console.WriteLine($"Question tracker already exists for connection {Context.ConnectionId}");
                return;
            }
        }
        
        try
        {
            var tracker = new InterviewQuestionTracker();
            var subTopicDescription = session.SubTopic.Description;
            var topicObjectives = session.SubTopic.Topic?.Objectives;
            var topicTitle = session.SubTopic.Topic?.Title;
            var subTopicTitle = session.SubTopic.Title;

            // Determine the language for question generation
            var isSpanish = session.Language == InterviewLanguage.Spanish;
            var languageInstruction = isSpanish 
                ? "Genera todas las preguntas en español. Responde solo en español."
                : "Generate all questions in English. Respond only in English.";

            // Build a detailed context section for the prompt
            var contextSection = $"Topic: {topicTitle}\n" +
                                $"Topic Objective: {topicObjectives}\n" +
                                $"Subtopic: {subTopicTitle}\n" +
                                $"Subtopic Objective: {subTopicDescription}\n";

            var prompt = $"{languageInstruction}\n\n" +
                         $"You are an expert technical interviewer. Use the following context to conduct an interview with the user.\n" +
                         $"{contextSection}\n\n" +
                         $"Generate exactly 10 unique and specific technical questions suitable for a candidate with {session.CandidateEducation} education and {session.CandidateExperience} years of experience.\n\n" +
                         "Requirements:\n" +
                         "- Generate exactly 10 questions\n" +
                         "- Make each question unique and specific to the topic and objectives above\n" +
                         "- Questions should vary in difficulty based on experience level\n" +
                         "- Include practical, theoretical, and problem-solving questions\n" +
                         "- Format as numbered list: 1. Question, 2. Question, etc.\n" +
                         "- Do not include any explanations, just the questions\n" +
                         "- Each question should be different from the others\n" +
                         $"- Focus on the specific topic: {subTopicTitle}\n" +
                         "- Use both the topic and subtopic objectives to guide your questions";

            Console.WriteLine($"Sending question generation prompt to AI: {prompt}");
            var questions = await _aiService.AskQuestionAsync(prompt);
            Console.WriteLine($"AI response for questions: {questions}");

            if (string.IsNullOrWhiteSpace(questions) || questions.Contains("No response received") || questions.Contains("Error") || questions.Contains("API Error"))
            {
                Console.WriteLine("AI failed to generate questions - cannot proceed with interview");
                throw new InvalidOperationException("Failed to generate interview questions. Please try again later.");
            }

            // Parse the AI response
            var lines = questions.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var parsedQuestions = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                // Try to extract question from numbered format (1. Question, 2. Question, etc.)
                var questionMatch = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"^\d+\.\s*(.+)$");
                if (questionMatch.Success)
                {
                    var question = questionMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(question) && question.Length > 5)
                    {
                        parsedQuestions.Add(question);
                    }
                }
                else if (trimmedLine.Contains("?") && trimmedLine.Length > 10)
                {
                    // If it looks like a question, add it
                    parsedQuestions.Add(trimmedLine);
                }
            }

            if (parsedQuestions.Count >= 10)
            {
                tracker.AvailableQuestions.AddRange(parsedQuestions.Take(10));
                Console.WriteLine($"Successfully parsed {tracker.AvailableQuestions.Count} questions from AI response");
            }
            else
            {
                Console.WriteLine($"Only parsed {parsedQuestions.Count} questions from AI response - cannot proceed");
                throw new InvalidOperationException($"Failed to generate enough questions. Only got {parsedQuestions.Count} questions. Please try again.");
            }

            lock (_trackerLock)
            {
                _questionTrackers.TryAdd(Context.ConnectionId, tracker);
            }
            Console.WriteLine($"Question tracker initialized with {tracker.AvailableQuestions.Count} questions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing question tracker: {ex}");
            throw; // Re-throw to be handled by the calling method
        }
    }

    public async Task SendAnswer(string answer)
    {
        try
        {
            if (!_sessions.TryGetValue(Context.ConnectionId, out var session))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "No active session found.");
                return;
            }

            // Save user message
            session.Messages.Add(new ChatMessage
            {
                Content = answer,
                IsUserMessage = true,
                SessionId = session.Id,
                Timestamp = DateTime.UtcNow
            });

            // Track consecutive non-answers
            var lastAnswer = answer?.Trim().ToLower() ?? "";
            var nonAnswers = new[] { "no", "i don't know", "idk", "n/a", "none", "not sure", "nope", "nil", "nothing", "no experience", "haven't", "haven’t", "don't have", "do not have", "can't say", "cannot say", "no answer", "no idea" ,"yes" };
            bool isNonAnswer = nonAnswers.Any(pattern => lastAnswer == pattern || lastAnswer.StartsWith(pattern + " ") || lastAnswer.Contains(pattern + ".") || lastAnswer.Contains(pattern + ",") || lastAnswer == pattern.Replace("'", ""));
            if (isNonAnswer)
            {
                _consecutiveNonAnswers.AddOrUpdate(Context.ConnectionId, 1, (key, old) => old + 1);
            }
            else
            {
                _consecutiveNonAnswers[Context.ConnectionId] = 0;
            }

            // If this is the first message (greeting), bot greets and asks first question
            if (session.CurrentQuestionNumber == 0)
            {
                var greetingMessage = session.Language == InterviewLanguage.Spanish
                    ? "¡Encantado de conocerte! Comencemos la entrevista."
                    : "Nice to meet you! Let's begin the interview.";
                
                await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", greetingMessage);
                
                try
                {
                    await AskNextQuestion(session);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error asking first question: {ex}");
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to start interview. Please try again.");
                }
                return;
            }

            // Continue with next question or check if objective is met
            await _db.SaveChangesAsync();
            
            try
            {
                await AskNextQuestion(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error asking next question: {ex}");
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to send message. Please try again.");
            }

            if (_exitOfferPending.TryGetValue(Context.ConnectionId, out var pending) && pending)
            {
                var userInput = answer?.Trim().ToLower() ?? "";
                var endPatterns = new[] { "no", "end", "stop", "finish", "terminate", "exit", "quit" };
                var continuePatterns = new[] { "yes", "continue", "go on", "keep going", "proceed" };
                if (endPatterns.Any(p => userInput == p || userInput.StartsWith(p + " ")))
                {
                    // End the interview immediately
                    _exitOfferPending[Context.ConnectionId] = false;
                    await CompleteInterviewManually();
                    return;
                }
                else if (continuePatterns.Any(p => userInput == p || userInput.StartsWith(p + " ")))
                {
                    // Clear the flag and proceed as normal
                    _exitOfferPending[Context.ConnectionId] = false;
                    // Continue to AskNextQuestion below
                }
                else
                {
                    // If ambiguous, repeat the offer
                    await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", "Please reply 'continue' to proceed or 'end' to finish the interview.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendAnswer: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to send message. Please try again.");
        }
    }

    public async Task CompleteInterviewManually()
    {
        if (!_sessions.TryGetValue(Context.ConnectionId, out var session))
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "No active session found.");
            return;
        }

        if (session.IsCompleted)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", "Interview already completed.");
            return;
        }

        try
        {
            Console.WriteLine($"Completing interview for session {session.Id}");
            Console.WriteLine($"In-memory session has {session.Messages.Count} messages");
            
            // First, ensure all messages from the in-memory session are saved to database
            foreach (var message in session.Messages)
            {
                if (message.Id == 0) // New message not yet saved
                {
                    _db.ChatMessages.Add(message);
                }
            }
            await _db.SaveChangesAsync();
            
            // Mark session as completed using raw SQL to avoid tracking issues
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE \"InterviewSessions\" SET \"IsCompleted\" = true, \"EndTime\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow, session.Id);

            // Get the updated session with messages
            var dbSession = await _db.InterviewSessions
                .Include(s => s.Messages)
                .Include(s => s.SubTopic)
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            if (dbSession == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Session not found in database.");
                return;
            }

            Console.WriteLine($"Retrieved session from database. Messages count: {dbSession.Messages.Count}");

            // Generate evaluation
            var isSpanish = dbSession.Language == InterviewLanguage.Spanish;
            var languageInstruction = isSpanish 
                ? "Evalúa esta entrevista en español. Responde solo en español. Si una respuesta es incorrecta o el usuario dice 'no lo sé', 'ninguna', 'no', 'n/a' o similar, asigna 0 puntos a esa pregunta y refleja esto en la puntuación final. Si todas las respuestas son de este tipo, la puntuación total debe ser 0/100, sin excepciones. No des ningún puntaje mínimo por participación o educación si ninguna respuesta técnica es correcta."
                : "Evaluate this interview in English. Respond only in English. If an answer is incorrect or the user says 'I don’t know', 'none', 'no', 'n/a' or similar, give 0 points for that question and reflect this in the final score. If all answers are of this type, the total score must be 0/100, no exceptions. Do not give any minimum score for participation or education if no technical answers are correct.";
            
            var evaluationPrompt = $"{languageInstruction}\n\n" +
                                  $"Evaluate this interview for {dbSession.CandidateName} about {dbSession.SubTopic.Title}.\n";
            evaluationPrompt += $"Candidate has {dbSession.CandidateEducation} education and {dbSession.CandidateExperience} years experience.\n";
            evaluationPrompt += "Questions and answers:\n";

            // Properly extract Q&A pairs by pairing questions with their answers
            var messages = dbSession.Messages.OrderBy(m => m.Timestamp).ToList();
            var qaPairs = new List<(string Question, string Answer)>();

            Console.WriteLine($"Total messages in session: {messages.Count}");
            
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                Console.WriteLine($"Message {i}: IsUser={message.IsUserMessage}, Content='{message.Content}'");
                
                if (!message.IsUserMessage) // This is a bot message
                {
                    Console.WriteLine($"Processing bot message: '{message.Content}'");
                    
                    // Only consider messages that are actual questions (contain "Question" or "Pregunta")
                    if (message.Content.Contains("Question") || message.Content.Contains("Pregunta"))
                    {
                        Console.WriteLine($"This is a question message: '{message.Content}'");
                        
                        // Find the next user message (answer) after this question
                        var answer = messages.Skip(i + 1)
                            .FirstOrDefault(m => m.IsUserMessage)?.Content ?? "No answer provided";
                        
                        Console.WriteLine($"Found Q&A pair: Q='{message.Content}', A='{answer}'");
                        qaPairs.Add((message.Content, answer));
                    }
                    else
                    {
                        Console.WriteLine($"Skipping non-question bot message: '{message.Content}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping user message: '{message.Content}'");
                }
            }

            Console.WriteLine($"Total Q&A pairs extracted: {qaPairs.Count}");

            // Use all Q&A pairs for comprehensive evaluation
            foreach (var (question, answer) in qaPairs)
            {
                // Truncate very long answers to avoid token limit but keep more content
                var truncatedAnswer = answer.Length > 500 ? answer.Substring(0, 500) + "..." : answer;
                evaluationPrompt += $"Q: {question}\nA: {truncatedAnswer}\n\n";
            }

            if (qaPairs.Count == 0)
            {
                evaluationPrompt += isSpanish 
                    ? "No se respondieron preguntas en esta entrevista.\n\n"
                    : "No questions were answered in this interview.\n\n";
            }

            evaluationPrompt += isSpanish
                ? "Proporciona una evaluación completa:\n1. Puntuación de 0 a 100 (Puntuación: XX)\n2. Análisis detallado de las respuestas\n3. Fortalezas identificadas\n4. Áreas de mejora\n5. Recomendaciones específicas"
                : "Provide a comprehensive evaluation:\n1. Score out of 100 (Score: XX)\n2. Detailed analysis of responses\n3. Identified strengths\n4. Areas for improvement\n5. Specific recommendations";

            Console.WriteLine($"Sending evaluation prompt to AI (length: {evaluationPrompt.Length} chars): {evaluationPrompt}");

            var evaluation = await _aiService.AskQuestionAsync(evaluationPrompt);
            Console.WriteLine($"AI evaluation response: {evaluation}");
            
            // Only use the agent's score, do not use fallback logic
            int score = 0;
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(evaluation, @"Score:\s*(\d+)");
            if (!scoreMatch.Success)
            {
                // Try Spanish format
                scoreMatch = System.Text.RegularExpressions.Regex.Match(evaluation, @"Puntuación:\s*(\d+)");
            }
            if (scoreMatch.Success)
            {
                int.TryParse(scoreMatch.Groups[1].Value, out score);
            }
            else
            {
                // If no score found, set to 0
                score = 0;
            }

            Console.WriteLine($"Parsed score: {score}");

            // Create and save result
            var result = new InterviewResult
            {
                SessionId = dbSession.Id,
                Score = score,
                Evaluation = evaluation,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var (question, answer) in qaPairs)
            {
                result.Questions.Add(new InterviewQuestion
                {
                    Question = question,
                    Answer = answer
                });
            }

            _db.InterviewResults.Add(result);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Saved result with {result.Questions.Count} questions");

            // Notify client and redirect directly to results
            await Clients.Caller.SendAsync("InterviewCompleted", score, evaluation);
            await Clients.Caller.SendAsync("RedirectToResults", dbSession.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing interview: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System",
                "Error completing interview. Please check the results page.");
        }
        finally
        {
            _sessions.TryRemove(Context.ConnectionId, out _);
            _questionTrackers.TryRemove(Context.ConnectionId, out _);
            _consecutiveNonAnswers.TryRemove(Context.ConnectionId, out _);
            _exitOfferPending.TryRemove(Context.ConnectionId, out _);
        }
    }

    private string GenerateFallbackEvaluation(List<(string Question, string Answer)> qaPairs, bool isSpanish, InterviewSession session)
    {
        // Calculate score based on answer quality
        var score = CalculateAnswerQualityScore(qaPairs);
        var performanceLevel = GetPerformanceLevel(score, isSpanish);
        
        if (isSpanish)
        {
            return $"Puntuación: {score}\n\n" +
                   $"Evaluación Detallada:\n" +
                   $"El candidato {session.CandidateName} completó una entrevista sobre {session.SubTopic.Title}.\n" +
                   $"Se respondieron {qaPairs.Count} preguntas de 10 totales.\n\n" +
                   $"Análisis de Respuestas:\n" +
                   $"Basado en la calidad de las respuestas proporcionadas, el candidato muestra un nivel {performanceLevel} de conocimiento en el área.\n\n" +
                   $"Fortalezas Identificadas:\n" +
                   $"- Participación activa en la entrevista\n" +
                   $"- Disposición para responder preguntas técnicas\n" +
                   $"- Experiencia previa en el campo ({session.CandidateExperience} años)\n\n" +
                   $"Áreas de Mejora:\n" +
                   $"- Continuar desarrollando conocimientos técnicos específicos\n" +
                   $"- Profundizar en conceptos avanzados de {session.SubTopic.Title}\n" +
                   $"- Practicar con proyectos más complejos\n" +
                   $"- Mejorar la calidad y detalle de las respuestas\n\n" +
                   $"Recomendaciones Específicas:\n" +
                   $"- Continuar estudiando los conceptos fundamentales de {session.SubTopic.Title}\n" +
                   $"- Practicar con proyectos prácticos y casos de uso reales\n" +
                   $"- Mantenerse actualizado con las últimas tendencias en el campo\n" +
                   $"- Considerar certificaciones relevantes para fortalecer el perfil profesional\n" +
                   $"- Trabajar en proporcionar respuestas más detalladas y específicas";
        }
        else
        {
            return $"Score: {score}\n\n" +
                   $"Detailed Evaluation:\n" +
                   $"The candidate {session.CandidateName} completed an interview about {session.SubTopic.Title}.\n" +
                   $"They answered {qaPairs.Count} out of 10 questions.\n\n" +
                   $"Response Analysis:\n" +
                   $"Based on the quality of the provided answers, the candidate shows a {performanceLevel} level of knowledge in this area.\n\n" +
                   $"Identified Strengths:\n" +
                   $"- Active participation in the interview\n" +
                   $"- Willingness to answer technical questions\n" +
                   $"- Previous experience in the field ({session.CandidateExperience} years)\n\n" +
                   $"Areas for Improvement:\n" +
                   $"- Continue developing specific technical knowledge\n" +
                   $"- Deepen understanding of advanced concepts in {session.SubTopic.Title}\n" +
                   $"- Practice with more complex projects\n" +
                   $"- Improve the quality and detail of responses\n\n" +
                   $"Specific Recommendations:\n" +
                   $"- Continue studying the fundamental concepts of {session.SubTopic.Title}\n" +
                   $"- Practice with hands-on projects and real-world use cases\n" +
                   $"- Stay updated with the latest trends in the field\n" +
                   $"- Consider relevant certifications to strengthen professional profile\n" +
                   $"- Work on providing more detailed and specific answers";
        }
    }

    private int CalculateAnswerQualityScore(List<(string Question, string Answer)> qaPairs)
    {
        if (qaPairs.Count == 0) return 0;
        
        var totalScore = 0;
        foreach (var (question, answer) in qaPairs)
        {
            var answerScore = EvaluateSingleAnswer(answer);
            totalScore += answerScore;
        }
        
        return Math.Min(100, totalScore / qaPairs.Count);
    }

    private int EvaluateSingleAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return 0;
        var trimmedAnswer = answer.Trim().ToLower();
        // Patterns for non-answers
        var nonAnswers = new[] { "no", "i don't know", "idk", "n/a", "none", "not sure", "nope", "nil", "nothing", "no experience", "haven't", "haven't", "don't have", "do not have", "can't say", "cannot say", "no answer", "no idea" };
        if (nonAnswers.Any(pattern =>
            trimmedAnswer == pattern ||
            trimmedAnswer.StartsWith(pattern + " ") ||
            trimmedAnswer.Contains(pattern + ".") ||
            trimmedAnswer.Contains(pattern + ",") ||
            trimmedAnswer == pattern.Replace("'", "")))
            return 0;
        // Check for nonsense answers (random characters, very short, etc.)
        if (trimmedAnswer.Length < 3) return 0;
        if (IsNonsenseAnswer(trimmedAnswer)) return 5;
        // Score based on answer length and content
        if (trimmedAnswer.Length < 10) return 10;
        if (trimmedAnswer.Length < 20) return 20;
        if (trimmedAnswer.Length < 50) return 40;
        if (trimmedAnswer.Length < 100) return 60;
        if (trimmedAnswer.Length < 200) return 80;
        return 90;
    }

    private bool IsNonsenseAnswer(string answer)
    {
        // Check for patterns that indicate nonsense answers
        var lowerAnswer = answer.ToLower();
        
        // Check for repeated characters (like "asdasd", "zxczxc")
        if (HasRepeatedPattern(lowerAnswer)) return true;
        
        // Check for very short random strings
        if (answer.Length < 5 && !answer.Contains(" ") && !answer.Contains(".")) return true;
        
        // Check for common nonsense patterns
        var nonsensePatterns = new[] { "asd", "zxc", "qwe", "tyu", "iop", "jkl", "bnm" };
        return nonsensePatterns.Any(pattern => lowerAnswer.Contains(pattern));
    }

    private bool HasRepeatedPattern(string text)
    {
        if (text.Length < 6) return false;
        
        // Check for 3+ character patterns that repeat
        for (int len = 3; len <= text.Length / 2; len++)
        {
            for (int i = 0; i <= text.Length - len * 2; i++)
            {
                var pattern = text.Substring(i, len);
                var nextOccurrence = text.IndexOf(pattern, i + len);
                if (nextOccurrence >= 0 && nextOccurrence < i + len + 2)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private string GetPerformanceLevel(int score, bool isSpanish)
    {
        if (score >= 80) return isSpanish ? "excelente" : "excellent";
        if (score >= 60) return isSpanish ? "bueno" : "good";
        if (score >= 40) return isSpanish ? "regular" : "fair";
        return isSpanish ? "necesita mejorar" : "needs improvement";
    }

    public async Task EndInterviewEarly()
    {
        if (!_sessions.TryGetValue(Context.ConnectionId, out var session))
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "No active session found.");
            return;
        }

        try
        {
            // Mark session as completed
            session.IsCompleted = true;
            session.EndTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify client
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Interview ended early. Your progress has been saved.");
            await Clients.Caller.SendAsync("RedirectToResults", session.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ending interview early: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System",
                "Error ending interview early. Please try again.");
        }
        finally
        {
            _sessions.TryRemove(Context.ConnectionId, out _);
            _questionTrackers.TryRemove(Context.ConnectionId, out _);
            _consecutiveNonAnswers.TryRemove(Context.ConnectionId, out _);
            _exitOfferPending.TryRemove(Context.ConnectionId, out _);
        }
    }

    private async Task AskNextQuestion(InterviewSession session)
    {
        try
        {
            if (session.IsCompleted) return;

            // Increment question number
            session.CurrentQuestionNumber++;

            // Build the conversation history for the prompt
            var messages = session.Messages.OrderBy(m => m.Timestamp).ToList();
            var isSpanish = session.Language == InterviewLanguage.Spanish;
            var topicTitle = session.SubTopic.Topic?.Title;
            var topicObjectives = session.SubTopic.Topic?.Objectives;
            var subTopicTitle = session.SubTopic.Title;
            var subTopicDescription = session.SubTopic.Description;

            var contextSection = $"Topic: {topicTitle}\n" +
                                $"Topic Objective: {topicObjectives}\n" +
                                $"Subtopic: {subTopicTitle}\n" +
                                $"Subtopic Objective: {subTopicDescription}\n";

            var languageInstruction = isSpanish
                ? "Eres un entrevistador técnico experto. Antes de hacer la siguiente pregunta, analiza cuidadosamente la respuesta anterior del candidato y evalúa si el objetivo de la entrevista se ha cumplido. Si el candidato responde con 'no', 'no lo sé', 'ninguna', o respuestas similares, adapta la siguiente pregunta para ser más sencilla, de apoyo, o cambia el enfoque a conceptos generales o motivacionales. Si el candidato muestra conocimiento, aumenta la dificultad. Nunca repitas preguntas. Si consideras que el objetivo se ha cumplido o que ya tienes suficiente información para evaluar al candidato, responde con 'OBJECTIVE_MET' en lugar de una pregunta. Si has llegado a la pregunta 10 y aún no has cumplido el objetivo, responde con 'OBJECTIVE_MET' para terminar la entrevista. Solo responde con la siguiente pregunta o 'OBJECTIVE_MET', sin explicaciones ni numeración."
                : "You are an expert technical interviewer. Before asking the next question, carefully interpret the candidate's previous answer and evaluate if the interview objective has been met. If the candidate responds with 'no', 'I don't know', 'none', 'n/a', or similar, adapt the next question to be simpler, more supportive, or shift to general or motivational topics. If the candidate shows knowledge, increase the difficulty. Never repeat questions. If you consider that the objective has been met or that you have sufficient information to evaluate the candidate, respond with 'OBJECTIVE_MET' instead of a question. If you have reached question 10 and still haven't met the objective, respond with 'OBJECTIVE_MET' to end the interview. Only output the next question or 'OBJECTIVE_MET', no explanations or numbering.";

            var prompt = new StringBuilder();
            prompt.AppendLine(languageInstruction);
            prompt.AppendLine();
            prompt.AppendLine(contextSection);
            prompt.AppendLine();
            prompt.AppendLine($"You are conducting a technical interview on the topic '{subTopicTitle}'.");
            prompt.AppendLine($"The candidate has {session.CandidateEducation} education and {session.CandidateExperience} years of experience.");
            prompt.AppendLine();
            prompt.AppendLine($"This is question {session.CurrentQuestionNumber}. Do not repeat previous questions.");
            prompt.AppendLine();
            prompt.AppendLine("Conversation so far:");

            int qNum = 1;
            foreach (var msg in messages)
            {
                if (!msg.IsUserMessage && (msg.Content.Contains("Question") || msg.Content.Contains("Pregunta")))
                {
                    prompt.AppendLine($"Q{qNum}: {msg.Content}");
                }
                else if (msg.IsUserMessage)
                {
                    prompt.AppendLine($"A{qNum}: {msg.Content}");
                    qNum++;
                }
            }
            prompt.AppendLine();
            prompt.AppendLine("Based on the above, interpret the candidate's previous answer(s) and adapt the next question to their level and previous answers. Only output the next question, no explanations or numbering.");

            int nonAnswerCount = _consecutiveNonAnswers.TryGetValue(Context.ConnectionId, out var count) ? count : 0;
            bool offerExit = nonAnswerCount >= 4;
            bool offerAlternative = nonAnswerCount >= 2 && nonAnswerCount < 4;

            if (offerExit)
            {
                var exitMsg = isSpanish
                    ? "He notado que has respondido varias veces que no tienes experiencia o no sabes. ¿Te gustaría terminar la entrevista aquí? Si deseas continuar, por favor indícalo."
                    : "I've noticed you've answered several times that you don't have experience or don't know. Would you like to end the interview here? If you'd like to continue, please let me know.";
                await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", exitMsg);
                _exitOfferPending[Context.ConnectionId] = true;
                return;
            }
            else if (offerAlternative)
            {
                var altMsg = isSpanish
                    ? "He notado que no tienes experiencia en este tema. ¿Te gustaría hablar de otra tecnología, tu experiencia general, o continuar con preguntas más generales? Si prefieres terminar la entrevista, también puedes indicarlo."
                    : "I've noticed you don't have experience in this topic. Would you like to talk about another technology, your general experience, or continue with more general questions? If you'd prefer to end the interview, you can also let me know.";
                await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", altMsg);
                // Continue with a more general/motivational question
            }

            // Call the AI to get the next question or determine if objective is met
            var aiResponse = await _aiService.AskQuestionAsync(prompt.ToString());
            if (string.IsNullOrWhiteSpace(aiResponse) || aiResponse.Contains("No response received") || aiResponse.Contains("Error"))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", isSpanish ? "No se pudo generar la siguiente pregunta. Intenta de nuevo." : "Failed to generate the next question. Please try again.");
                return;
            }

            // Check if AI determined objective is met
            if (aiResponse.Trim().ToUpper() == "OBJECTIVE_MET")
            {
                var completionMessage = isSpanish
                    ? "Perfecto. He evaluado tus respuestas y considero que hemos cubierto suficientemente el objetivo de esta entrevista. Procederé a generar tu evaluación."
                    : "Perfect. I have evaluated your responses and consider that we have sufficiently covered the objective of this interview. I will now generate your evaluation.";
                
                await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", completionMessage);
                
                // Complete the interview
                await CompleteInterviewManually();
                return;
            }

            // Check if we've reached the maximum of 10 questions and force completion
            if (session.CurrentQuestionNumber >= 10)
            {
                var maxQuestionsMessage = isSpanish
                    ? "Hemos llegado al límite de 10 preguntas para esta entrevista. Procederé a generar tu evaluación basada en las respuestas proporcionadas."
                    : "We have reached the limit of 10 questions for this interview. I will now generate your evaluation based on the provided responses.";
                
                await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", maxQuestionsMessage);
                
                // Complete the interview
                await CompleteInterviewManually();
                return;
            }

            // Clean up AI response: remove leading numbering or 'Question X:' or 'Pregunta X:'
            string cleanedQuestion = aiResponse.Trim();
            cleanedQuestion = System.Text.RegularExpressions.Regex.Replace(cleanedQuestion, @"^(Question|Pregunta)?\s*\d+\s*[:\.]?\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleanedQuestion = System.Text.RegularExpressions.Regex.Replace(cleanedQuestion, @"^\d+\s*[\.:\)]?\s*", "");

            // Format question in the appropriate language
            var questionPrefix = isSpanish
                ? $"Pregunta {session.CurrentQuestionNumber}: "
                : $"Question {session.CurrentQuestionNumber}: ";
            var formattedQuestion = questionPrefix + cleanedQuestion;
            await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", formattedQuestion);

            session.Messages.Add(new ChatMessage
            {
                Content = formattedQuestion,
                IsUserMessage = false,
                SessionId = session.Id,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AskNextQuestion: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Error generating question. Please try again.");
        }
    }

    public async Task ResumeInterview(int sessionId)
    {
        try
        {
            // Check if there's already an active session
            if (_sessions.ContainsKey(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Interview already in progress");
                return;
            }

            // Get the session from database
            var session = await _db.InterviewSessions
                .Include(s => s.SubTopic)
                    .ThenInclude(st => st.Topic)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Session not found");
                return;
            }

            // Check if session is already completed
            if (session.IsCompleted)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "This interview is already completed");
                return;
            }

            // Add session to active sessions
            _sessions.TryAdd(Context.ConnectionId, session);
            
            try
            {
                await InitializeQuestionTracker(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize question tracker for resume: {ex}");
                _sessions.TryRemove(Context.ConnectionId, out _);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to generate interview questions. Please try starting a new interview.");
                return;
            }

            var welcomeBackMessage = session.Language == InterviewLanguage.Spanish
                ? $"¡Bienvenido de vuelta! Continuemos tu entrevista sobre {session.SubTopic.Title}. ¿Dónde nos quedamos?"
                : $"Welcome back! Let's continue your interview on {session.SubTopic.Title}. Where were we?";

            await Clients.Caller.SendAsync("ReceiveMessage", "Interviewer", welcomeBackMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resuming interview: {ex}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Failed to resume interview. Please try again.");
        }
    }
}