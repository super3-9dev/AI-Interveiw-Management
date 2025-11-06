using Microsoft.EntityFrameworkCore;
using InterviewBot.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
        public DbSet<InterviewAnalysisResult> InterviewAnalysisResults { get; set; }
        public DbSet<InterviewNote> InterviewNotes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupTask> Tasks { get; set; }
        public DbSet<GroupResource> Resources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            // Configure User-InterviewResult relationship (one-to-many)
            modelBuilder.Entity<InterviewResult>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Note: InterviewCatalog-InterviewResult relationship temporarily disabled
            // due to data type mismatch. Will be re-enabled after data cleanup.

            // Configure User-ChatMessage relationship
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InterviewResult-InterviewNote relationship (one-to-many)
            modelBuilder.Entity<InterviewNote>()
                .HasOne(n => n.InterviewResult)
                .WithMany()
                .HasForeignKey(n => n.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure DateTime properties for InterviewNote
            modelBuilder.Entity<InterviewNote>(entity =>
            {
                entity.Property(e => e.Date)
                    .HasConversion(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                
                entity.Property(e => e.CreatedAt)
                    .HasConversion(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                
                entity.Property(e => e.UpdatedAt)
                    .HasConversion(
                        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
            });

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
                .Property(r => r.Content)
                .HasMaxLength(5000);

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

            // Configure User-Group relationship
            modelBuilder.Entity<Group>()
                .HasOne(g => g.User)
                .WithMany(u => u.Groups)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Group entity properties
            modelBuilder.Entity<Group>(entity =>
            {
                entity.Property(g => g.Name).IsRequired().HasMaxLength(200);
                entity.Property(g => g.Description).HasMaxLength(1000);
                entity.Property(g => g.StudentCount)
                    .HasDefaultValue(0);
                // Configure Emails column - map to "emails" column name (lowercase) if it exists
                entity.Property(g => g.Emails)
                    .HasMaxLength(5000) // Store emails separated by newlines
                    .HasColumnName("emails"); // Map to lowercase column name if it exists in DB
                // Ignore the computed property
                entity.Ignore(g => g.StudentCountValue);
            });

            // Configure Group-Task relationship
            modelBuilder.Entity<GroupTask>()
                .HasOne(t => t.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GroupTask entity properties
            modelBuilder.Entity<GroupTask>(entity =>
            {
                entity.ToTable("Tasks"); // Map to "Tasks" table
                
                // Configure Id as identity column for PostgreSQL
                entity.Property(t => t.Id)
                    .ValueGeneratedOnAdd();
                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(entity.Property(t => t.Id));
                
                // Map properties to database columns
                entity.Property(t => t.AgentName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("AgentName");
                
                entity.Property(t => t.TaskName)
                    .IsRequired()
                    .HasMaxLength(255);
                
                entity.Property(t => t.Instructions)
                    .HasMaxLength(255);
                
                entity.Property(t => t.Objective)
                    .HasMaxLength(255);
                
                entity.Property(t => t.Constraints)
                    .HasMaxLength(255);
                
                entity.Property(t => t.Emphasis)
                    .HasMaxLength(255);
                
                entity.Property(t => t.IsVisible)
                    .HasColumnName("IsVisible")
                    .IsRequired()
                    .HasDefaultValue(false);
                
                // Ignore computed property
                entity.Ignore(t => t.Title);
            });

            // Configure Group-Resource relationship
            modelBuilder.Entity<GroupResource>()
                .HasOne(r => r.Group)
                .WithMany(g => g.Resources)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GroupResource entity properties
            modelBuilder.Entity<GroupResource>(entity =>
            {
                entity.ToTable("Resources"); // Keep table name as "Resources" for backward compatibility
                entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Url).IsRequired().HasMaxLength(500);
            });

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

            // Configure InterviewAnalysisResult relationships
            modelBuilder.Entity<InterviewAnalysisResult>()
                .HasOne(ar => ar.User)
                .WithMany()
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure string field lengths for InterviewAnalysisResult
            modelBuilder.Entity<InterviewAnalysisResult>()
                .Property(ar => ar.Summary)
                .HasMaxLength(2000);

            modelBuilder.Entity<InterviewAnalysisResult>()
                .Property(ar => ar.Recommendations)
                .HasMaxLength(2000);

            modelBuilder.Entity<InterviewAnalysisResult>()
                .Property(ar => ar.MBAFocusArea)
                .HasMaxLength(100);
        }
    }
}