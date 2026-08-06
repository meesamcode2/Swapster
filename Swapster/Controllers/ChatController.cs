using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        public async Task<IActionResult> Conversation(int userId)
        {
            int? currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUserId.Value == userId)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            var otherUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (otherUser == null)
            {
                return NotFound();
            }

            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId.Value &&
                     m.ReceiverId == userId)
                    ||
                    (m.SenderId == userId &&
                     m.ReceiverId == currentUserId.Value))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var unreadMessages = messages
                .Where(m =>
                    m.ReceiverId == currentUserId.Value &&
                    !m.IsRead)
                .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            var model = new ChatViewModel
            {
                CurrentUserId = currentUserId.Value,
                OtherUserId = otherUser.UserId,
                OtherUsername = otherUser.Username,
                Messages = messages
            };

            return View(model);
        }

        public async Task<IActionResult> Index()
        {
            int? currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    m.SenderId == currentUserId.Value ||
                    m.ReceiverId == currentUserId.Value)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversationUsers = messages
                .Select(m => m.SenderId == currentUserId.Value
                    ? m.Receiver
                    : m.Sender)
                .Where(u => u != null)
                .GroupBy(u => u!.UserId)
                .Select(g => g.First()!)
                .ToList();

            return View(conversationUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Send(
            int receiverId,
            string content)
        {
            int? senderId = GetCurrentUserId();

            if (senderId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(
                    "Conversation",
                    new { userId = receiverId });
            }

            if (senderId.Value == receiverId)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            var receiverExists = await _context.Users
                .AnyAsync(u => u.UserId == receiverId);

            if (!receiverExists)
            {
                return NotFound();
            }

            var message = new Message
            {
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Conversation",
                new { userId = receiverId });
        }
    }
}