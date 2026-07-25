using System.Collections.Generic;

namespace Swapster.Models
{
    public class MyAccountViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<UserSkill> MySkills { get; set; } = new();
        public List<UserWantedSkill> WantedSkills { get; set; } = new();
        public List<ProfileRecommendationViewModel> RecommendedProfiles { get; set; } = new();
    }

    public class ProfileRecommendationViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TheyTeach { get; set; } = string.Empty;
        public string YouTeach { get; set; } = string.Empty;
        public string MatchType { get; set; } = string.Empty;
    }
}