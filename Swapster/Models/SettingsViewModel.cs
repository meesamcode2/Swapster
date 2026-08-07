namespace Swapster.Models
{
    public class SettingsViewModel
    {
        public string Username { get; set; } = "User";
        public string Email { get; set; } = "Not available";

        public bool PublicProfile { get; set; } = true;
        public bool ShowMySkills { get; set; } = true;

        public bool EmailNotifications { get; set; } = true;
        public bool SkillRequestAlerts { get; set; } = true;
        public bool RecommendationUpdates { get; set; } = true;

        public bool ShowEmailToOthers { get; set; } = false;
        public bool AllowMatchSuggestions { get; set; } = true;
        public bool ProfileSearchVisibility { get; set; } = true;

        public string ThemeMode { get; set; } = "Light";
        public string BrandColor { get; set; } = "Default";
        public bool CompactDashboard { get; set; } = false;

        public bool EnableAiAssistant { get; set; } = true;
        public bool PersonalizedRecommendations { get; set; } = true;
        public bool CareerGuidance { get; set; } = true;
    }
}
