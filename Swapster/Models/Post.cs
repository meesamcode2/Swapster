using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Swapster.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        public int UserId { get; set; }

        // Text is optional because an image-only post is allowed.
        [StringLength(2000)]
        public string? Content { get; set; }

        [StringLength(300)]
        public string? ImagePath { get; set; }

        [StringLength(300)]
        public string? VideoPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<PostLike> Likes { get; set; }
            = new List<PostLike>();

        public ICollection<PostComment> Comments { get; set; }
            = new List<PostComment>();

        public ICollection<PostShare> Shares { get; set; }
            = new List<PostShare>();
    }
}