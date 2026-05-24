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
        public DbSet<UserWantedSkill> UserWantedSkills { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SkillRequests -> Sender (User) relation
            modelBuilder.Entity<SkillRequest>()
                .HasOne(sr => sr.Sender)
                .WithMany()
                .HasForeignKey(sr => sr.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // <- IMPORTANT

            // SkillRequests -> Receiver (User) relation
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
        }
    }
}