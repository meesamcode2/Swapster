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

        // Send Request
        public IActionResult Send(int skillId, int receiverId)
        {
            int? senderId = HttpContext.Session.GetInt32("UserId");

            if (senderId == null)
                return RedirectToAction("Login", "Account");

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

        // View My Requests
        public IActionResult MyRequests()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var requests = _context.SkillRequests
                .Include(r => r.Skill)
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Where(r => r.SenderId == userId || r.ReceiverId == userId)
                .ToList();

            return View(requests);
        }

        public IActionResult Accept(int id)
        {
            var request = _context.SkillRequests.Find(id);
            request.Status = "Accepted";
            _context.SaveChanges();

            return RedirectToAction("MyRequests");
        }

        public IActionResult Reject(int id)
        {
            var request = _context.SkillRequests.Find(id);
            request.Status = "Rejected";
            _context.SaveChanges();

            return RedirectToAction("MyRequests");
        }
    }
}