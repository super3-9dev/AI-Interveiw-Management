using System.Text.Json.Serialization;

namespace InterviewBot.Models
{
    public class StudentReportResponse
    {
        [JsonPropertyName("studentData")]
        public StudentData StudentData { get; set; } = new();
    }

    public class StudentData
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("processDate")]
        public string ProcessDate { get; set; } = string.Empty;

        [JsonPropertyName("interviews")]
        public List<InterviewData> Interviews { get; set; } = new();
    }

    public class InterviewData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }
}
