using System.Text.Json.Serialization;

namespace InterviewBot.Models
{
    public class InterviewAnalysisResponse
    {
        [JsonPropertyName("response")]
        public InterviewAnalysisData Response { get; set; } = new();
    }

    public class InterviewAnalysisData
    {
        [JsonPropertyName("catalog")]
        public InterviewCatalogData Catalog { get; set; } = new();
    }

    public class InterviewCatalogData
    {
        [JsonPropertyName("InterviewSummary")]
        public InterviewSummary InterviewSummary { get; set; } = new();

        [JsonPropertyName("MBAFocusArea")]
        public string MBAFocusArea { get; set; } = string.Empty;

        [JsonPropertyName("YourCareerRoadmaps")]
        public List<CareerRoadmap> YourCareerRoadmaps { get; set; } = new();

        [JsonPropertyName("AdditionalTips")]
        public List<string> AdditionalTips { get; set; } = new();

        [JsonPropertyName("clarityScore")]
        public int ClarityScore { get; set; }
    }

    public class InterviewSummary
    {
        [JsonPropertyName("Summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("Recommendations")]
        public string Recommendations { get; set; } = string.Empty;
    }

    public class CareerRoadmap
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<string> Steps { get; set; } = new();
    }
}
