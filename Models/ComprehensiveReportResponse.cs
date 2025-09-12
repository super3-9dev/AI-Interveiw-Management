using System.Text.Json.Serialization;

namespace InterviewBot.Models
{
    public class ComprehensiveReportResponse
    {
        [JsonPropertyName("response")]
        public ReportData Response { get; set; } = new();
    }

    public class ReportData
    {
        [JsonPropertyName("clientInfo")]
        public ClientInfo ClientInfo { get; set; } = new();

        [JsonPropertyName("initialAssessments")]
        public InitialAssessments InitialAssessments { get; set; } = new();

        [JsonPropertyName("progressTracking")]
        public ProgressTracking ProgressTracking { get; set; } = new();

        [JsonPropertyName("wellbeingAssessments")]
        public WellbeingAssessments WellbeingAssessments { get; set; } = new();

        [JsonPropertyName("reflectionLog")]
        public List<ReflectionEntry> ReflectionLog { get; set; } = new();

        [JsonPropertyName("feedbackAssessments")]
        public FeedbackAssessments FeedbackAssessments { get; set; } = new();
    }

    public class ClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("processStartDate")]
        public string ProcessStartDate { get; set; } = string.Empty;
    }

    public class InitialAssessments
    {
        [JsonPropertyName("wheelOfLife")]
        public WheelOfLife WheelOfLife { get; set; } = new();

        [JsonPropertyName("personalityDISC")]
        public PersonalityDISC PersonalityDISC { get; set; } = new();

        [JsonPropertyName("strengths")]
        public Strengths Strengths { get; set; } = new();

        [JsonPropertyName("qualitativeAnalysis")]
        public QualitativeAnalysis QualitativeAnalysis { get; set; } = new();
    }

    public class WheelOfLife
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("currentWheelData")]
        public List<int> CurrentWheelData { get; set; } = new();

        [JsonPropertyName("idealWheelData")]
        public List<int> IdealWheelData { get; set; } = new();
    }

    public class PersonalityDISC
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }

    public class Strengths
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }

    public class QualitativeAnalysis
    {
        [JsonPropertyName("personalContext")]
        public PersonalContext PersonalContext { get; set; } = new();

        [JsonPropertyName("perceptions")]
        public Perceptions Perceptions { get; set; } = new();

        [JsonPropertyName("skills")]
        public Skills Skills { get; set; } = new();

        [JsonPropertyName("attitudes")]
        public Attitudes Attitudes { get; set; } = new();
    }

    public class PersonalContext
    {
        [JsonPropertyName("history")]
        public string History { get; set; } = string.Empty;

        [JsonPropertyName("motivations")]
        public string Motivations { get; set; } = string.Empty;

        [JsonPropertyName("values")]
        public string Values { get; set; } = string.Empty;
    }

    public class Perceptions
    {
        [JsonPropertyName("learning")]
        public string Learning { get; set; } = string.Empty;

        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("challenges")]
        public string Challenges { get; set; } = string.Empty;
    }

    public class Skills
    {
        [JsonPropertyName("strategies")]
        public string Strategies { get; set; } = string.Empty;

        [JsonPropertyName("communication")]
        public string Communication { get; set; } = string.Empty;

        [JsonPropertyName("selfAssessment")]
        public string SelfAssessment { get; set; } = string.Empty;
    }

    public class Attitudes
    {
        [JsonPropertyName("satisfaction")]
        public string Satisfaction { get; set; } = string.Empty;

        [JsonPropertyName("emotionalState")]
        public string EmotionalState { get; set; } = string.Empty;

        [JsonPropertyName("future")]
        public string Future { get; set; } = string.Empty;
    }

    public class ProgressTracking
    {
        [JsonPropertyName("goals")]
        public List<Goal> Goals { get; set; } = new();

        [JsonPropertyName("habitTracker")]
        public HabitTracker HabitTracker { get; set; } = new();
    }

    public class Goal
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("percentage")]
        public int Percentage { get; set; }
    }

    public class HabitTracker
    {
        [JsonPropertyName("habitName")]
        public string HabitName { get; set; } = string.Empty;

        [JsonPropertyName("weeklyStatus")]
        public List<bool> WeeklyStatus { get; set; } = new();
    }

    public class WellbeingAssessments
    {
        [JsonPropertyName("wellbeingTrend")]
        public WellbeingTrend WellbeingTrend { get; set; } = new();

        [JsonPropertyName("keyEmotionsTrend")]
        public KeyEmotionsTrend KeyEmotionsTrend { get; set; } = new();

        [JsonPropertyName("sentimentTrend")]
        public SentimentTrend SentimentTrend { get; set; } = new();

        [JsonPropertyName("emotionMap")]
        public EmotionMap EmotionMap { get; set; } = new();

        [JsonPropertyName("monthlyEmotionLog")]
        public MonthlyEmotionLog MonthlyEmotionLog { get; set; } = new();

        [JsonPropertyName("sentimentAnalysis")]
        public SentimentAnalysis SentimentAnalysis { get; set; } = new();

        [JsonPropertyName("keyEmotionAnalysis")]
        public List<KeyEmotionAnalysis> KeyEmotionAnalysis { get; set; } = new();
    }

    public class WellbeingTrend
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("datasets")]
        public List<WellbeingDataset> Datasets { get; set; } = new();
    }

    public class WellbeingDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<double> Data { get; set; } = new();
    }

    public class KeyEmotionsTrend
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("datasets")]
        public List<KeyEmotionsDataset> Datasets { get; set; } = new();
    }

    public class KeyEmotionsDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<double> Data { get; set; } = new();
    }

    public class SentimentTrend
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("data")]
        public List<double> Data { get; set; } = new();
    }

    public class EmotionMap
    {
        [JsonPropertyName("datasets")]
        public List<EmotionMapDataset> Datasets { get; set; } = new();
    }

    public class EmotionMapDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<EmotionDataPoint> Data { get; set; } = new();
    }

    public class EmotionDataPoint
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("r")]
        public int R { get; set; }
    }

    public class MonthlyEmotionLog
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }

    public class SentimentAnalysis
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class KeyEmotionAnalysis
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public int Score { get; set; }
    }

    public class ReflectionEntry
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }

    public class FeedbackAssessments
    {
        [JsonPropertyName("feedback360")]
        public Feedback360 Feedback360 { get; set; } = new();

        [JsonPropertyName("competency")]
        public Competency Competency { get; set; } = new();
    }

    public class Feedback360
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("datasets")]
        public List<FeedbackDataset> Datasets { get; set; } = new();
    }

    public class FeedbackDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }

    public class Competency
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("datasets")]
        public List<CompetencyDataset> Datasets { get; set; } = new();
    }

    public class CompetencyDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<int> Data { get; set; } = new();
    }
}
