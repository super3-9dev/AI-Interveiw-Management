using System.Net.Mail;
using System.Net;
using InterviewBot.Models;

namespace InterviewBot.Services
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public interface IEmailService
    {
        Task SendInterviewInviteAsync(SubTopic subTopic, string? culture = null);
        Task SendVerificationEmailAsync(string email, string verificationCode, string? culture = null);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _logger = logger;
            _configuration = config;
            _config = new EmailConfig
            {
                SmtpServer = config["Email:SmtpServer"] ?? string.Empty,
                SmtpPort = int.TryParse(config["Email:SmtpPort"], out var port) ? port : 587,
                SmtpUsername = config["Email:SmtpUsername"] ?? string.Empty,
                SmtpPassword = config["Email:SmtpPassword"] ?? string.Empty,
                FromEmail = config["Email:FromEmail"] ?? string.Empty,
                FromName = config["Email:FromName"] ?? "InterviewBot"
            };
        }

        public async Task SendInterviewInviteAsync(SubTopic subTopic, string? culture = null)
        {
            if (string.IsNullOrEmpty(subTopic.CandidateEmail))
            {
                _logger.LogWarning("No candidate email provided for subtopic {SubTopicId}", subTopic.Id);
                return;
            }

            try
            {
                var emailAddresses = subTopic.CandidateEmail
                    .Split(';', ',')
                    .Select(email => email.Trim())
                    .Where(email => !string.IsNullOrEmpty(email))
                    .ToList();

                if (!emailAddresses.Any())
                {
                    _logger.LogWarning("No valid email addresses found in candidate email: {CandidateEmail}", subTopic.CandidateEmail);
                    return;
                }

                var subject = culture == "es" ? $"Invitación de Entrevista: {subTopic.Title}" : $"Interview Invitation: {subTopic.Title}";
                var body = GenerateInterviewInviteEmail(subTopic, culture);

                // Try multiple SMTP configurations
                var smtpConfigs = new[]
                {
                    new { Server = "smtp.gmail.com", Port = 587, UseSsl = true },
                    new { Server = "smtp.gmail.com", Port = 465, UseSsl = true },
                    new { Server = "smtp.gmail.com", Port = 25, UseSsl = false }
                };

                Exception? lastException = null;

                foreach (var smtpConfig in smtpConfigs)
                {
                    try
                    {
                        using var client = new SmtpClient(smtpConfig.Server, smtpConfig.Port)
                        {
                            EnableSsl = smtpConfig.UseSsl,
                            Credentials = new NetworkCredential(_config.SmtpUsername, _config.SmtpPassword),
                            Timeout = 15000, // 15 seconds timeout
                            DeliveryMethod = SmtpDeliveryMethod.Network
                        };

                        var message = new MailMessage
                        {
                            From = new MailAddress(_config.FromEmail, _config.FromName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        };

                        foreach (var email in emailAddresses)
                        {
                            message.To.Add(email);
                        }

                        await client.SendMailAsync(message);
                        _logger.LogInformation("Interview invitation sent successfully for subtopic {SubTopicId} to {EmailCount} recipients using {Server}:{Port}", 
                            subTopic.Id, emailAddresses.Count, smtpConfig.Server, smtpConfig.Port);
                        return; // Success, exit the loop
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning("Failed to send email using {Server}:{Port} for subtopic {SubTopicId}: {Error}", 
                            smtpConfig.Server, smtpConfig.Port, subTopic.Id, ex.Message);
                        continue; // Try next configuration
                    }
                }

                // If we get here, all configurations failed
                throw lastException ?? new Exception("All SMTP configurations failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview invitation for subtopic {SubTopicId}", subTopic.Id);
                throw;
            }
        }

        public async Task SendVerificationEmailAsync(string email, string verificationCode, string? culture = null)
        {
            try
            {
                var subject = culture == "es" ? "InterviewBot - Código de Verificación de Email" : "InterviewBot - Email Verification Code";
                var body = GenerateVerificationEmail(verificationCode, culture);

                // Try multiple SMTP configurations
                var smtpConfigs = new[]
                {
                    new { Server = "smtp.gmail.com", Port = 587, UseSsl = true },
                    new { Server = "smtp.gmail.com", Port = 465, UseSsl = true },
                    new { Server = "smtp.gmail.com", Port = 25, UseSsl = false }
                };

                Exception? lastException = null;

                foreach (var smtpConfig in smtpConfigs)
                {
                    try
                    {
                        using var client = new SmtpClient(smtpConfig.Server, smtpConfig.Port)
                        {
                            EnableSsl = smtpConfig.UseSsl,
                            Credentials = new NetworkCredential(_config.SmtpUsername, _config.SmtpPassword),
                            Timeout = 15000, // 15 seconds timeout
                            DeliveryMethod = SmtpDeliveryMethod.Network
                        };

                        var message = new MailMessage
                        {
                            From = new MailAddress(_config.FromEmail, _config.FromName),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        };

                        message.To.Add(email);

                        await client.SendMailAsync(message);
                        _logger.LogInformation("Verification email sent successfully to {Email} using {Server}:{Port}", 
                            email, smtpConfig.Server, smtpConfig.Port);
                        return; // Success, exit the loop
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning("Failed to send verification email using {Server}:{Port} to {Email}: {Error}", 
                            smtpConfig.Server, smtpConfig.Port, email, ex.Message);
                        continue; // Try next configuration
                    }
                }

                // If we get here, all configurations failed
                throw lastException ?? new Exception("All SMTP configurations failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", email);
                throw;
            }
        }

        private string GenerateInterviewInviteEmail(SubTopic subTopic, string? culture = null)
        {
            var baseUrl = _configuration.GetValue<string>("AppSettings:BaseUrl") ?? "http://localhost:5000";
            return $@"
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            background: #f4f6fb;
                            margin: 0;
                            padding: 0;
                            font-family: 'Segoe UI', Arial, sans-serif;
                        }}
                        .container {{
                            max-width: 420px;
                            margin: 32px auto;
                            background: #fff;
                            border-radius: 12px;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.07);
                            overflow: hidden;
                        }}
                        .header {{
                            background: #1976d2;
                            color: #fff;
                            padding: 24px 0 18px 0;
                            text-align: center;
                        }}
                        .header h2 {{
                            margin: 0;
                            font-size: 2rem;
                            font-weight: 700;
                        }}
                        .content {{
                            padding: 32px 28px 24px 28px;
                            text-align: center;
                        }}
                        .content h3 {{
                            margin-top: 0;
                            font-size: 1.3rem;
                            font-weight: 700;
                        }}
                        .content p {{
                            color: #444;
                            font-size: 1.05rem;
                            margin: 12px 0 18px 0;
                        }}
                        .btn {{
                            display: inline-block;
                            background: #2979ff;
                            color: #fff !important;
                            text-decoration: none;
                            font-weight: 600;
                            padding: 12px 32px;
                            border-radius: 7px;
                            font-size: 1.1rem;
                            margin: 18px 0 0 0;
                            transition: background 0.2s;
                        }}
                        .btn:hover {{
                            background: #1565c0;
                        }}
                        .footer {{
                            background: #f4f6fb;
                            color: #888;
                            font-size: 0.93rem;
                            text-align: center;
                            padding: 18px 16px 12px 16px;
                            border-top: 1px solid #e0e0e0;
                        }}
                        @media (max-width: 500px) {{
                            .container {{
                                max-width: 98vw;
                                margin: 10px;
                            }}
                            .content {{
                                padding: 18px 6vw 18px 6vw;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>{(culture == "es" ? "Invitación de Entrevista" : "Interview Invitation")}</h2>
                        </div>
                        <div class='content'>
                            <h3>{(culture == "es" ? "¡Hola!" : "Hello!")}</h3>
                            <p>{(culture == "es" ? "Tienes una entrevista para completar." : "You have an interview to complete.")}</p>
                            <p>
                                {(culture == "es" ? "Por favor haz clic en el botón de abajo para comenzar tu sesión de entrevista en línea." : "Please click the button below to begin your online interview session.")}
                            </p>
                            <a class='btn' href='{baseUrl}/EmailVerification?subTopicId={subTopic.Id}'>
                                {(culture == "es" ? "Comenzar Entrevista" : "Start Interview")}
                            </a>
                        </div>
                        <div class='footer'>
                            {(culture == "es" ? "Si tienes alguna pregunta, por favor contacta a nuestro equipo de soporte." : "If you have any questions, please contact our support team.")}<br/>
                            &copy; 2024 Your Company Inc. All rights reserved.
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateVerificationEmail(string verificationCode, string? culture = null)
        {
            return $@"
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            background: #f4f6fb;
                            margin: 0;
                            padding: 0;
                            font-family: 'Segoe UI', Arial, sans-serif;
                        }}
                        .container {{
                            max-width: 420px;
                            margin: 32px auto;
                            background: #fff;
                            border-radius: 12px;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.07);
                            overflow: hidden;
                        }}
                        .header {{
                            background: #1976d2;
                            color: #fff;
                            padding: 24px 0 18px 0;
                            text-align: center;
                        }}
                        .header h2 {{
                            margin: 0;
                            font-size: 2rem;
                            font-weight: 700;
                        }}
                        .content {{
                            padding: 32px 28px 24px 28px;
                            text-align: center;
                        }}
                        .verification-code {{
                            background: #f8f9fa;
                            border: 2px solid #dee2e6;
                            border-radius: 8px;
                            padding: 20px;
                            margin: 20px 0;
                            font-size: 2rem;
                            font-weight: bold;
                            letter-spacing: 0.5rem;
                            color: #1976d2;
                        }}
                        .content p {{
                            color: #444;
                            font-size: 1.05rem;
                            margin: 12px 0 18px 0;
                        }}
                        .footer {{
                            background: #f4f6fb;
                            color: #888;
                            font-size: 0.93rem;
                            text-align: center;
                            padding: 18px 16px 12px 16px;
                            border-top: 1px solid #e0e0e0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>{(culture == "es" ? "Verificación de Email" : "Email Verification")}</h2>
                        </div>
                        <div class='content'>
                            <h3>{(culture == "es" ? "Tu Código de Verificación" : "Your Verification Code")}</h3>
                            <p>{(culture == "es" ? "Por favor ingresa este código para verificar tu dirección de email e iniciar tu entrevista." : "Please enter this code to verify your email address and start your interview.")}</p>
                            <div class='verification-code'>{verificationCode}</div>
                            <p>{(culture == "es" ? "Este código expirará en 10 minutos." : "This code will expire in 10 minutes.")}</p>
                        </div>
                        <div class='footer'>
                            {(culture == "es" ? "Saludos cordiales," : "Best regards,")}<br/>InterviewBot Team
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
} 