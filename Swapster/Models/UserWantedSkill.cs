using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserWantedSkill
    {
        [Key]
        public int WantedSkillId { get; set; }

        [Required]
        public string SkillName { get; set; } = string.Empty; // e.g., "JavaScript"

        // NEW: Kis level tak sikhna chahta hai (1 to 5)
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int DesiredLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Standardized to UtcNow

        // Foreign Key
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}