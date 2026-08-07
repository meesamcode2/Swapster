using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserSetting
    {
        [Key]
        public int UserSettingId { get; set; }

        [Required]
        public int UserId { get; set; }

        public bool PublicProfile { get; set; } = true;
        public bool ShowMySkills { get; set; } = true;

        public bool EmailNotifications { get; set; } = true;
        public bool SkillRequestAlerts { get; set; } = true;
        public bool RecommendationUpdates { get; set; } = true;

        public bool ShowEmailToOthers { get; set; } = false;
        public bool AllowMatchSuggestions { get; set; } = true;
        public bool ProfileSearchVisibility { get; set; } = true;

        [MaxLength(20)]
        public string ThemeMode { get; set; } = "Light";

        [MaxLength(30)]
        public string BrandColor { get; set; } = "Default";

        public bool CompactDashboard { get; set; } = false;

        public bool EnableAiAssistant { get; set; } = true;
        public bool PersonalizedRecommendations { get; set; } = true;
        public bool CareerGuidance { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
