using System.Collections.Generic;

namespace Swapster.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;

        public int CompletedSwaps { get; set; }
        public int IncomingRequests { get; set; }
        public int OutgoingRequests { get; set; }
        public int TotalRequests => IncomingRequests + OutgoingRequests;
        public List<UserSkill> PostedSkills { get; set; } = new();
        public List<SkillRequest> SkillRequests { get; set; } = new();
        public List<UserSkill> RecommendedSkills { get; set; } = new();
        public List<Post> CommunityPosts { get; set; } = new();
    }
}