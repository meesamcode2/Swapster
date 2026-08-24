using Microsoft.EntityFrameworkCore;
using Swapster.Models;

namespace Swapster.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserSkill> UserSkills { get; set; } = null!;
        public DbSet<SkillRequest> SkillRequests { get; set; } = null!;
        public DbSet<UserWantedSkill> UserWantedSkills { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<UserAvailability> UserAvailabilities { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;

        public DbSet<PostLike> PostLikes { get; set; } = null!;
        public DbSet<PostComment> PostComments { get; set; } = null!;
        public DbSet<PostShare> PostShares { get; set; } = null!;

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // SKILL REQUEST RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<SkillRequest>()
                .HasOne(request => request.Sender)
                .WithMany()
                .HasForeignKey(request => request.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillRequest>()
                .HasOne(request => request.Receiver)
                .WithMany()
                .HasForeignKey(request => request.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillRequest>()
                .HasOne(request => request.Skill)
                .WithMany()
                .HasForeignKey(request => request.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // USER PROFILE RELATIONSHIP
            // One user should have one profile.
            // =====================================================

            modelBuilder.Entity<UserProfile>()
                .HasOne(profile => profile.User)
                .WithMany()
                .HasForeignKey(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(profile => profile.UserId)
                .IsUnique();

            // =====================================================
            // USER AVAILABILITY RELATIONSHIP
            // One user can have multiple day/time availability rows.
            // =====================================================

            modelBuilder.Entity<UserAvailability>()
                .HasOne(availability => availability.User)
                .WithMany()
                .HasForeignKey(availability => availability.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAvailability>()
                .HasIndex(availability =>
                    new
                    {
                        availability.UserId,
                        availability.DayOfWeek,
                        availability.StartTime,
                        availability.EndTime
                    });

            // =====================================================
            // MESSAGE RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<Message>()
                .HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(message => message.Receiver)
                .WithMany()
                .HasForeignKey(message => message.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // COMMUNITY POST RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<Post>()
                .HasOne(post => post.User)
                .WithMany()
                .HasForeignKey(post => post.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One user can like one post only once.
            modelBuilder.Entity<PostLike>()
                .HasIndex(like =>
                    new
                    {
                        like.PostId,
                        like.UserId
                    })
                .IsUnique();

            modelBuilder.Entity<PostLike>()
                .HasOne(like => like.Post)
                .WithMany(post => post.Likes)
                .HasForeignKey(like => like.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostLike>()
                .HasOne(like => like.User)
                .WithMany()
                .HasForeignKey(like => like.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostComment>()
                .HasOne(comment => comment.Post)
                .WithMany(post => post.Comments)
                .HasForeignKey(comment => comment.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostComment>()
                .HasOne(comment => comment.User)
                .WithMany()
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostShare>()
                .HasOne(share => share.Post)
                .WithMany(post => post.Shares)
                .HasForeignKey(share => share.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostShare>()
                .HasOne(share => share.User)
                .WithMany()
                .HasForeignKey(share => share.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}