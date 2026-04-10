using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        public string ExperienceLevel { get; set; } // Beginner, Intermediate, Expert

        public string Skills { get; set; } // comma separated

        public string Bio { get; set; }

        public string Interests { get; set; }
    }
}