using InterviewBot.Data;
using InterviewBot.Models;
using InterviewBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewBot.Pages.InterviewSessions
{
    [Authorize]
    public class ExportModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly PdfService _pdfService;

        public ExportModel(AppDbContext db, PdfService pdfService)
        {
            _db = db;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var session = await _db.InterviewSessions
                .Include(s => s.SubTopic)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.Result)
                    .ThenInclude(r => r!.Questions)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null || !session.IsCompleted)
            {
                return NotFound();
            }

            var score = session.Result?.Score ?? 0;
            var performance = score >= 80 ? "Excellent" : score >= 60 ? "Good" : "Needs Improvement";

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            line-height: 1.4;
                            color: #333;
                            margin: 0;
                            padding: 20px;
                            background-color: white;
                        }}
                        
                        .header {{
                            text-align: center;
                            border-bottom: 2px solid #007bff;
                            padding-bottom: 20px;
                            margin-bottom: 30px;
                        }}
                        
                        .header h1 {{
                            color: #007bff;
                            margin: 0;
                            font-size: 24px;
                            font-weight: bold;
                        }}
                        
                        .header .subtitle {{
                            color: #666;
                            margin-top: 5px;
                            font-size: 14px;
                        }}
                        
                        .score-section {{
                            text-align: center;
                            margin-bottom: 30px;
                            padding: 20px;
                            border: 1px solid #ddd;
                            border-radius: 5px;
                        }}
                        
                        .score {{
                            font-size: 36px;
                            font-weight: bold;
                            color: #007bff;
                            margin-bottom: 10px;
                        }}
                        
                        .performance {{
                            font-size: 16px;
                            color: #666;
                            margin-bottom: 15px;
                        }}
                        
                        .topic {{
                            font-size: 18px;
                            font-weight: bold;
                            color: #333;
                            margin-bottom: 5px;
                        }}
                        
                        .date {{
                            font-size: 12px;
                            color: #666;
                        }}
                        
                        .info-section {{
                            margin-bottom: 30px;
                        }}
                        
                        .info-section h3 {{
                            color: #007bff;
                            border-bottom: 1px solid #ddd;
                            padding-bottom: 5px;
                            margin-bottom: 15px;
                            font-size: 16px;
                        }}
                        
                        .info-grid {{
                            display: table;
                            width: 100%;
                            border-collapse: collapse;
                        }}
                        
                        .info-row {{
                            display: table-row;
                        }}
                        
                        .info-label {{
                            display: table-cell;
                            font-weight: bold;
                            padding: 8px 0;
                            width: 30%;
                            color: #555;
                        }}
                        
                        .info-value {{
                            display: table-cell;
                            padding: 8px 0;
                            color: #333;
                        }}
                        
                        .evaluation-section {{
                            margin-bottom: 30px;
                        }}
                        
                        .evaluation-section h3 {{
                            color: #007bff;
                            border-bottom: 1px solid #ddd;
                            padding-bottom: 5px;
                            margin-bottom: 15px;
                            font-size: 16px;
                        }}
                        
                        .evaluation-content {{
                            background-color: #f9f9f9;
                            padding: 15px;
                            border-left: 4px solid #007bff;
                            line-height: 1.6;
                        }}
                        
                        .qa-section {{
                            margin-bottom: 30px;
                        }}
                        
                        .qa-section h3 {{
                            color: #007bff;
                            border-bottom: 1px solid #ddd;
                            padding-bottom: 5px;
                            margin-bottom: 15px;
                            font-size: 16px;
                        }}
                        
                        .question-item {{
                            margin-bottom: 20px;
                            border: 1px solid #ddd;
                            border-radius: 5px;
                            overflow: hidden;
                        }}
                        
                        .question-header {{
                            background-color: #f5f5f5;
                            padding: 10px 15px;
                            font-weight: bold;
                            color: #333;
                            border-bottom: 1px solid #ddd;
                        }}
                        
                        .question-content {{
                            padding: 15px;
                        }}
                        
                        .answer-label {{
                            font-weight: bold;
                            color: #007bff;
                            margin-bottom: 5px;
                            font-size: 14px;
                        }}
                        
                        .answer-text {{
                            background-color: #f9f9f9;
                            padding: 10px;
                            border-radius: 3px;
                            margin-bottom: 10px;
                            line-height: 1.5;
                        }}
                        
                        .feedback-label {{
                            font-weight: bold;
                            color: #28a745;
                            margin-bottom: 5px;
                            font-size: 14px;
                        }}
                        
                        .feedback-text {{
                            background-color: #d4edda;
                            padding: 10px;
                            border-radius: 3px;
                            color: #155724;
                            line-height: 1.5;
                        }}
                        
                        .footer {{
                            text-align: center;
                            margin-top: 40px;
                            padding-top: 20px;
                            border-top: 1px solid #ddd;
                            color: #666;
                            font-size: 12px;
                        }}
                        
                        @media print {{
                            body {{ 
                                background-color: white; 
                                margin: 0;
                                padding: 15px;
                            }}
                            .question-item {{
                                page-break-inside: avoid;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Interview Report</h1>
                        <div class='subtitle'>Professional Assessment & Evaluation</div>
                    </div>

                    <div class='score-section'>
                        <div class='score'>{score}/100</div>
                        <div class='performance'>{performance} Performance</div>
                        <div class='topic'>{session.SubTopic.Title}</div>
                        <div class='date'>Completed on {session.EndTime?.ToString("MMMM dd, yyyy 'at' h:mm tt")}</div>
                    </div>

                    <div class='info-section'>
                        <h3>Candidate Information</h3>
                        <div class='info-grid'>
                            <div class='info-row'>
                                <div class='info-label'>Name:</div>
                                <div class='info-value'>{session.CandidateName}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>Email:</div>
                                <div class='info-value'>{session.CandidateEmail}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>Education:</div>
                                <div class='info-value'>{session.CandidateEducation}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>Experience:</div>
                                <div class='info-value'>{session.CandidateExperience} years</div>
                            </div>
                        </div>
                    </div>

                    <div class='info-section'>
                        <h3>Interview Details</h3>
                        <div class='info-grid'>
                            <div class='info-row'>
                                <div class='info-label'>Topic:</div>
                                <div class='info-value'>{session.SubTopic.Topic.Title}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>SubTopic:</div>
                                <div class='info-value'>{session.SubTopic.Title}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>Duration:</div>
                                <div class='info-value'>{(session.EndTime - session.StartTime)?.ToString(@"hh\:mm\:ss")}</div>
                            </div>
                            <div class='info-row'>
                                <div class='info-label'>Questions:</div>
                                <div class='info-value'>{session.Result?.Questions?.Count ?? 0} answered</div>
                            </div>
                        </div>
                    </div>";

            if (!string.IsNullOrEmpty(session.Result?.Evaluation))
            {
                html += $@"
                    <div class='evaluation-section'>
                        <h3>Detailed Evaluation</h3>
                        <div class='evaluation-content'>
                            {session.Result.Evaluation.Replace("\n", "<br>")}
                        </div>
                    </div>";
            }

            html += @"
                    <div class='qa-section'>
                        <h3>Questions & Answers</h3>";

            if (session.Result?.Questions != null && session.Result.Questions.Any())
            {
                foreach (var qa in session.Result.Questions)
                {
                    html += $@"
                        <div class='question-item'>
                            <div class='question-header'>
                                Question {session.Result.Questions.IndexOf(qa) + 1}
                            </div>
                            <div class='question-content'>
                                <div style='margin-bottom: 15px;'>{qa.Question}</div>
                                <div class='answer-label'>Your Answer:</div>
                                <div class='answer-text'>{qa.Answer}</div>";

                    if (!string.IsNullOrEmpty(qa.Feedback))
                    {
                        html += $@"
                                <div class='feedback-label'>Feedback:</div>
                                <div class='feedback-text'>{qa.Feedback}</div>";
                    }

                    html += @"
                            </div>
                        </div>";
                }
            }
            else
            {
                html += @"
                    <div style='text-align: center; padding: 40px; color: #666;'>
                        <p>No questions available for this session.</p>
                    </div>";
            }

            html += $@"
                    </div>

                    <div class='footer'>
                        <p>Generated by InterviewBot on {DateTime.Now.ToString("MMMM dd, yyyy 'at' h:mm tt")}</p>
                        <p>This report contains confidential information and should be handled appropriately.</p>
                    </div>
                </body>
                </html>";

            try
            {
                var pdfBytes = _pdfService.GeneratePdf(html);
                return File(pdfBytes, "application/pdf", $"InterviewReport_{session.CandidateName}_{session.SubTopic.Title}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF generation error: {ex}");
                return BadRequest("Failed to generate PDF");
            }
        }
    }
}