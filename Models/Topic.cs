namespace InterviewBot.Models
{
    public class Topic
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Objectives { get; set; }
        public List<SubTopic> SubTopics { get; set; } = new();
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}