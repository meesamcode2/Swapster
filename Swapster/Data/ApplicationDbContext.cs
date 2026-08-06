using Microsoft.EntityFrameworkCore;
using Swapster.Models;

namespace Swapster.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserSkill> UserSkills { get; set; } = null!;
        public DbSet<SkillRequest> SkillRequests { get; set; } = null!;
        public DbSet<UserWantedSkill> UserWantedSkills { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SkillRequest>()
                .HasOne(sr => sr.Sender)
                .WithMany()
                .HasForeignKey(sr => sr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillRequest>()
                .HasOne(sr => sr.Receiver)
                .WithMany()
                .HasForeignKey(sr => sr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SkillRequest>()
                .HasOne(sr => sr.Skill)
                .WithMany()
                .HasForeignKey(sr => sr.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
    .           OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
               .HasOne(m => m.Receiver)
               .WithMany()
               .HasForeignKey(m => m.ReceiverId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}