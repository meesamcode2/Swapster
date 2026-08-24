using System;
using System.Collections.Generic;

namespace Swapster.Models
{
    public class MyAccountViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Editable profile information.
        public string Bio { get; set; } = string.Empty;
        public string Interests { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;

        public List<AvailabilityDayViewModel> Availability { get; set; } = new();

        public List<UserSkill> MySkills { get; set; } = new();
        public List<UserWantedSkill> WantedSkills { get; set; } = new();
        public List<ProfileRecommendationViewModel> RecommendedProfiles { get; set; } = new();
    }

    public class AvailabilityDayViewModel
    {
        public int DayOfWeek { get; set; }

        public string DayName { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
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