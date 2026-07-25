using System;

namespace Swapster.Models
{
    public class SkillRequest
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int SkillId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;
        public UserSkill Skill { get; set; } = null!;
    }
}