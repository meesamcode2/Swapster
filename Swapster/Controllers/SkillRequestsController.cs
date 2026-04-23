using Microsoft.AspNetCore.Mvc;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class SkillRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SEND REQUEST
        public IActionResult Send(int receiverId, int skillId) // 👈 CHANGE
        {
            int? senderId = HttpContext.Session.GetInt32("UserId");

            if (senderId == null)
                return RedirectToAction("Login", "Account");

            var request = new SkillRequest
            {
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                SkillId = skillId // 👈 FIXED
            };

            _context.SkillRequests.Add(request);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        // ACCEPT
        public IActionResult Accept(int id)
        {
            var req = _context.SkillRequests.Find(id);
            if (req != null)
            {
                req.Status = "Accepted";
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        // REJECT
        public IActionResult Reject(int id)
        {
            var req = _context.SkillRequests.Find(id);
            if (req != null)
            {
                req.Status = "Rejected";
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}