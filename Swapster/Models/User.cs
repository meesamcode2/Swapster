using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int AccessLevel { get; set; } = 2; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEmailVerified { get; set; } = false;

        public string? VerificationToken { get; set; }
        public DateTime? TokenExpiry { get; set; }

        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public ICollection<UserWantedSkill> UserWantedSkills { get; set; } = new List<UserWantedSkill>();
    }
}