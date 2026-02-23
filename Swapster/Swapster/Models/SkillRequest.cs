using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class SkillRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public int SkillId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        // Allowed: Pending, Accepted, Rejected

        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; } = null!;

        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; } = null!;

        [ForeignKey(nameof(SkillId))]
        public UserSkill Skill { get; set; } = null!;
    }
}