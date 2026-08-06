using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserSkill
    {
        [Key]
        public int SkillId { get; set; }

        [Required, StringLength(50)]
        public string SkillName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(1, 5)]
        public int ProficiencyLevel { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public string? AdminNote { get; set; }
    }
}