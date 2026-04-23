using System;

namespace Swapster.Models
{
    public class SkillRequest
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int SkillId { get; set; }   // 👈 ADD THIS

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 🔥 Navigation Properties (VERY IMPORTANT)
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public UserSkill Skill { get; set; }
    }
}