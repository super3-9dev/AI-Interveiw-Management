using Microsoft.EntityFrameworkCore;
using InterviewBot.Models;

namespace InterviewBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
        public DbSet<InterviewResult> InterviewResults => Set<InterviewResult>();
        public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<User> Users { get; set; }
        public DbSet<AIAgentRole> AIAgentRoles { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<InterviewCatalog> InterviewCatalogs { get; set; }
        public DbSet<CustomInterview> CustomInterviews { get; set; }
        public DbSet<InterviewCatalogItem> InterviewCatalogItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


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

                // Configure relationship with AIAgentRole
                entity.HasOne(u => u.SelectedAIAgentRole)
                    .WithMany()
                    .HasForeignKey(u => u.SelectedAIAgentRoleId)
                    .OnDelete(DeleteBehavior.SetNull);
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



            // Configure User-Profile relationship
            modelBuilder.Entity<Profile>()
                .HasOne(r => r.User)
                .WithMany(u => u.Profiles)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-InterviewCatalog relationship
            modelBuilder.Entity<InterviewCatalog>()
                .HasOne(ic => ic.User)
                .WithMany(u => u.InterviewCatalogs)
                .HasForeignKey(ic => ic.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-CustomInterview relationship
            modelBuilder.Entity<CustomInterview>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.CustomInterviews)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            // Configure InterviewSession relationships
            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.InterviewCatalog)
                .WithMany(ic => ic.InterviewSessions)
                .HasForeignKey(s => s.InterviewCatalogId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.CustomInterview)
                .WithMany(ci => ci.InterviewSessions)
                .HasForeignKey(s => s.CustomInterviewId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.AIAgentRole)
                .WithMany()
                .HasForeignKey(s => s.AIAgentRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}