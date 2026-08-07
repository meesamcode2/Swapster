using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class PostShare
    {
        [Key]
        public int PostShareId { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}