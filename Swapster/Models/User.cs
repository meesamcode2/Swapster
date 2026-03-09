using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int AccessLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}