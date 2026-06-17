namespace Swapster.Models
{
    public class AddSkillViewModel
    {
        // 1. Jo skill user OFFER kar raha hai
        public string OfferedSkillName { get; set; } = string.Empty;
        public int ProficiencyLevel { get; set; }
        public string Description { get; set; } = string.Empty;

        // 2. Jo skill user SEEKHNA (Want) chahta hai
        public string WantedSkillName { get; set; } = string.Empty;
        public int DesiredLevel { get; set; }
    }
}