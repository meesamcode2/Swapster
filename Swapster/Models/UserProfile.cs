using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty;

        [StringLength(500)]
        public string Skills { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(500)]
        public string Interests { get; set; } = string.Empty;

        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [StringLength(30)]
        public string Gender { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}