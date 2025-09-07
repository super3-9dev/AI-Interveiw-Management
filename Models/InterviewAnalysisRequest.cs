using System.Text.Json.Serialization;

namespace InterviewBot.Models
{
    public class InterviewAnalysisRequest
    {
        [JsonPropertyName("purpose")]
        public string Purpose { get; set; } = string.Empty;

        [JsonPropertyName("responseLanguage")]
        public string ResponseLanguage { get; set; } = "en";

        [JsonPropertyName("InterviewName")]
        public string InterviewName { get; set; } = string.Empty;

        [JsonPropertyName("InterviewObjective")]
        public string InterviewObjective { get; set; } = string.Empty;

        [JsonPropertyName("userProfileBrief")]
        public string UserProfileBrief { get; set; } = string.Empty;

        [JsonPropertyName("userProfileStrength")]
        public string UserProfileStrength { get; set; } = string.Empty;

        [JsonPropertyName("userProfileWeakness")]
        public string UserProfileWeakness { get; set; } = string.Empty;

        [JsonPropertyName("userProfileFutureCareerGoal")]
        public string UserProfileFutureCareerGoal { get; set; } = string.Empty;

        [JsonPropertyName("userProfileMotivation")]
        public string UserProfileMotivation { get; set; } = string.Empty;

        [JsonPropertyName("interviewConversation")]
        public List<InterviewConversation> InterviewConversation { get; set; } = new();

        [JsonPropertyName("InterviewId")]
        public string InterviewId { get; set; } = string.Empty;
    }

    public class InterviewConversation
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}
