using Microsoft.EntityFrameworkCore;
using InterviewBot.Models;

namespace InterviewBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<SubTopic> SubTopics => Set<SubTopic>();
        public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
        public DbSet<InterviewResult> InterviewResults => Set<InterviewResult>();
        public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Topic-SubTopic relationship
            modelBuilder.Entity<Topic>()
                .HasMany(t => t.SubTopics)
                .WithOne(st => st.Topic)
                .HasForeignKey(st => st.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SubTopic-InterviewSession relationship
            modelBuilder.Entity<SubTopic>()
                .HasMany(st => st.InterviewSessions)
                .WithOne(s => s.SubTopic)
                .HasForeignKey(s => s.SubTopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InterviewSession-InterviewResult relationship (one-to-one)
            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.Result)
                .WithOne(r => r.Session)
                .HasForeignKey<InterviewResult>(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InterviewResult-InterviewQuestion relationship (one-to-many)
            modelBuilder.Entity<InterviewResult>()
                .HasMany(r => r.Questions)
                .WithOne(q => q.Result)
                .HasForeignKey(q => q.InterviewResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InterviewSession-ChatMessage relationship
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(100);
                entity.Property(u => u.Education).HasMaxLength(100);
                entity.Property(u => u.Experience).HasMaxLength(50);
                entity.Property(u => u.CurrentPosition).HasMaxLength(100);
            });

            // Configure string field lengths
            modelBuilder.Entity<InterviewSession>()
                .Property(s => s.CandidateName)
                .HasMaxLength(100);

            modelBuilder.Entity<InterviewSession>()
                .Property(s => s.CandidateEmail)
                .HasMaxLength(100);

            modelBuilder.Entity<InterviewSession>()
                .Property(s => s.CandidateEducation)
                .HasMaxLength(100);

            modelBuilder.Entity<InterviewSession>()
                .Property(s => s.CandidateExperience)
                .HasMaxLength(50);

            modelBuilder.Entity<InterviewResult>()
                .Property(r => r.Evaluation)
                .HasMaxLength(4000);

            modelBuilder.Entity<InterviewQuestion>()
                .Property(q => q.Question)
                .HasMaxLength(1000);

            modelBuilder.Entity<InterviewQuestion>()
                .Property(q => q.Answer)
                .HasMaxLength(2000);

            modelBuilder.Entity<InterviewQuestion>()
                .Property(q => q.Feedback)
                .HasMaxLength(2000);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Content)
                .HasMaxLength(2000);

            // Configure Topic-User relationship
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SubTopic-User relationship
            modelBuilder.Entity<SubTopic>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}