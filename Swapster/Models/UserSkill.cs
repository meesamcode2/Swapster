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

        [Required]
        public string Description { get; set; } = string.Empty;

        // Foreign Key
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}