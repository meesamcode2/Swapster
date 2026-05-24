namespace Swapster.Models
{
    public class MyAccountViewModel
    {
        public User? User { get; set; }

        public List<UserSkill> MySkills { get; set; } = new List<UserSkill>();

        public List<UserWantedSkill> WantedSkills { get; set; } = new List<UserWantedSkill>();

        public List<ProfileRecommendationViewModel> RecommendedProfiles { get; set; } = new List<ProfileRecommendationViewModel>();
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