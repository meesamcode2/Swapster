using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserWantedSkill
    {
        [Key]
        public int WantedSkillId { get; set; }

        [Required, StringLength(50)]
        public string SkillName { get; set; } = string.Empty;

        [Required, Range(1, 5)]
        public int DesiredLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}