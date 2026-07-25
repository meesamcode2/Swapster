using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class AddSkillViewModel
    {
        [Required(ErrorMessage = "Please specify the skill you want to offer.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Skill name must be between 2 and 50 characters.")]
        public string OfferedSkillName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Proficiency level must be between 1 and 5.")]
        public int ProficiencyLevel { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify the skill you want to learn.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Skill name must be between 2 and 50 characters.")]
        public string WantedSkillName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Desired level must be between 1 and 5.")]
        public int DesiredLevel { get; set; }
    }
}