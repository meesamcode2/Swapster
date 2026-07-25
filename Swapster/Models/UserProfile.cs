using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ExperienceLevel { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string Interests { get; set; } = string.Empty;
    }
}