using System.Collections.Generic;

namespace Swapster.Models
{
    public class AdminUsersViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;

        public string RoleFilter { get; set; } = "all";

        public string StatusFilter { get; set; } = "all";

        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int BlockedUsers { get; set; }

        public int AdminUsers { get; set; }

        public int CurrentAdminId { get; set; }

        public List<AdminUserRowViewModel> Users { get; set; } = new();
    }

    public class AdminUserRowViewModel
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int AccessLevel { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsBlocked { get; set; }

        public DateTime CreatedAt { get; set; }

        public int OfferedSkillsCount { get; set; }

        public int WantedSkillsCount { get; set; }

        public int CompletedExchangesCount { get; set; }
    }

    public class AdminUserDetailsViewModel
    {
        public User User { get; set; } = null!;

        public List<UserSkill> OfferedSkills { get; set; } = new();

        public List<UserWantedSkill> WantedSkills { get; set; } = new();

        public List<SkillRequest> Exchanges { get; set; } = new();

        public int CurrentAdminId { get; set; }

        public int SentRequestsCount { get; set; }

        public int ReceivedRequestsCount { get; set; }

        public int CompletedExchangesCount { get; set; }
    }
}