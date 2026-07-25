using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class SkillRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        [HttpPost]
        public async Task<IActionResult> Send(int receiverId, int skillId)
        {
            int? senderId = GetCurrentUserId();
            if (senderId == null) return RedirectToAction("Login", "Account");

            if (senderId.Value == receiverId) return RedirectToAction("Index", "Home");

            bool requestExists = await _context.SkillRequests
                .AnyAsync(r => r.SenderId == senderId.Value && r.ReceiverId == receiverId && r.SkillId == skillId && r.Status == "Pending");

            if (requestExists) return RedirectToAction("Index", "Home");

            var request = new SkillRequest
            {
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                SkillId = skillId,
                Status = "Pending"
            };

            _context.SkillRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var req = await _context.SkillRequests.FindAsync(id);

            if (req != null && req.ReceiverId == userId.Value)
            {
                req.Status = "Accepted";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var req = await _context.SkillRequests.FindAsync(id);

            if (req != null && req.ReceiverId == userId.Value)
            {
                req.Status = "Rejected";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}