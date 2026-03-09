using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;

namespace Shoes_project.Controllers
{
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SEND REQUEST
        public IActionResult Send(int skillId, int receiverId)
        {
            int? senderId = HttpContext.Session.GetInt32("UserId");

            if (senderId == null)
                return RedirectToAction("Login", "Account");

            // Prevent duplicate requests
            bool exists = _context.SkillRequests.Any(r =>
                r.SkillId == skillId &&
                r.SenderId == senderId &&
                r.ReceiverId == receiverId &&
                r.Status == "Pending"
            );

            if (exists)
            {
                TempData["Error"] = "You already sent a request for this skill.";
                return RedirectToAction("MyRequests");
            }

            SkillRequest request = new SkillRequest
            {
                SkillId = skillId,
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                Status = "Pending"
            };

            _context.SkillRequests.Add(request);
            _context.SaveChanges();

            return RedirectToAction("MyRequests");
        }

        // VIEW MY REQUESTS
        public IActionResult MyRequests()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var requests = _context.SkillRequests
                .Include(r => r.Skill)
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Where(r => r.SenderId == userId || r.ReceiverId == userId)
                .OrderByDescending(r => r.RequestId)
                .ToList();

            return View(requests);
        }

        // ACCEPT REQUEST
        public IActionResult Accept(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var request = _context.SkillRequests.Find(id);

            if (request == null)
                return NotFound();

            // Only the receiver can accept the request
            if (request.ReceiverId != userId)
                return Unauthorized();

            request.Status = "Accepted";
            _context.SaveChanges();

            return RedirectToAction("MyRequests");
        }

        // REJECT REQUEST
        public IActionResult Reject(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var request = _context.SkillRequests.Find(id);

            if (request == null)
                return NotFound();

            // Only the receiver can reject the request
            if (request.ReceiverId != userId)
                return Unauthorized();

            request.Status = "Rejected";
            _context.SaveChanges();

            return RedirectToAction("MyRequests");
        }
    }
}