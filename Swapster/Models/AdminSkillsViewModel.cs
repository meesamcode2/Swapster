using System;
using System.Collections.Generic;

namespace Swapster.Models
{
    public class AdminSkillsViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;

        public string TypeFilter { get; set; } = "all";

        public int LevelFilter { get; set; }

        public string StatusFilter { get; set; } = "all";

        public string SortOrder { get; set; } = "latest";

        public int TotalOfferedSkills { get; set; }

        public int TotalWantedSkills { get; set; }

        public int ActiveSkills { get; set; }

        public int InactiveSkills { get; set; }

        public int DeletedSkills { get; set; }

        public List<AdminOfferedSkillRowViewModel> OfferedSkills { get; set; }
            = new();

        public List<AdminWantedSkillRowViewModel> WantedSkills { get; set; }
            = new();
    }

    public class AdminOfferedSkillRowViewModel
    {
        public int SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int ProficiencyLevel { get; set; }

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public string? AdminNote { get; set; }

        public int RequestCount { get; set; }
    }

    public class AdminWantedSkillRowViewModel
    {
        public int WantedSkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public int DesiredLevel { get; set; }

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public string? AdminNote { get; set; }
    }

    public class AdminSkillDetailsViewModel
    {
        public string SkillType { get; set; } = string.Empty;

        public int SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Level { get; set; }

        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public string? AdminNote { get; set; }

        public int TotalRequests { get; set; }

        public int PendingRequests { get; set; }

        public int AcceptedRequests { get; set; }

        public int RejectedRequests { get; set; }

        public int CompletedRequests { get; set; }

        public List<SkillRequest> Exchanges { get; set; } = new();
    }

    public class AdminEditSkillViewModel
    {
        public string SkillType { get; set; } = string.Empty;

        public int SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Level { get; set; }
    }

    public class AdminDeleteSkillViewModel
    {
        public string SkillType { get; set; } = string.Empty;

        public int SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public string AdminNote { get; set; } = string.Empty;
    }
}