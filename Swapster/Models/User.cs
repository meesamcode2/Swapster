using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Swapster.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // 1 = Admin, 2 = Normal User
        public int AccessLevel { get; set; } = 2;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // =====================================================
        // EMAIL VERIFICATION
        // =====================================================

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        // =====================================================
        // PASSWORD RESET
        // =====================================================

        public string? VerificationToken { get; set; }

        public DateTime? TokenExpiry { get; set; }

        // =====================================================
        // ACCOUNT STATUS
        // =====================================================

        public bool IsBlocked { get; set; } = false;

        public bool IsDeactivated { get; set; } = false;

        public DateTime? DeactivatedAt { get; set; }

        // =====================================================
        // NAVIGATION PROPERTIES
        // =====================================================

        public ICollection<UserSkill> UserSkills { get; set; }
            = new List<UserSkill>();

        public ICollection<UserWantedSkill> UserWantedSkills { get; set; }
            = new List<UserWantedSkill>();
    }
}