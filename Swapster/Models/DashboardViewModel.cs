using System.Collections.Generic;

namespace Swapster.Models
    {

    public class DashboardViewModel
        {
            public string UserName { get; set; } = string.Empty;

            public int TotalSkills { get; set; }
            public int TotalRequests { get; set; }
            public int CompletedSwaps { get; set; }

            public int IncomingRequests { get; set; }
            public int OutgoingRequests { get; set; }

            public List<UserSkill> MySkills { get; set; } = new();

            public List<UserSkill> PostedSkills { get; set; } = new();
            public List<SkillRequest> SkillRequests { get; set; } = new();
            public List<UserSkill> RecommendedSkills { get; set; } = new();
        }
    }
