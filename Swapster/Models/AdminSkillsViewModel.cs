using System.Collections.Generic;

namespace Swapster.Models
{
    public class AdminSkillsViewModel
    {
        public List<UserSkill> OfferedSkills { get; set; } = new();
        public List<UserWantedSkill> WantedSkills { get; set; } = new();
    }
}