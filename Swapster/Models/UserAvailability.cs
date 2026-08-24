using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class UserAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

       
        [Range(0, 6)]
        public int DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}