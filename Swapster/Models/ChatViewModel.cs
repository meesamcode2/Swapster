using System.Collections.Generic;

namespace Swapster.Models
{
    public class ChatViewModel
    {
        public int CurrentUserId { get; set; }

        public int OtherUserId { get; set; }

        public string OtherUsername { get; set; } = string.Empty;

        public List<Message> Messages { get; set; } = new();
    }
}