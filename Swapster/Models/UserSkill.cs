using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserSkill
    {
        [Key]
        public int SkillId { get; set; }

        [Required]
        public string SkillName { get; set; } = string.Empty; 

        public string Description { get; set; } = string.Empty;


        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int ProficiencyLevel { get; set; }

        // Foreign Key
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}