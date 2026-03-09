using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System.Diagnostics;

namespace Swapster.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string username = HttpContext.Session.GetString("Username");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var mySkills = _context.UserSkills
                .Where(s => s.UserId == userId)
                .ToList();

            var req = _context.SkillRequests
                .Where(r => r.SenderId == userId || r.ReceiverId == userId)
                .ToList();

            var model = new DashboardViewModel
            {
                UserName = username,
                TotalSkills = mySkills.Count,
                TotalRequests = req.Count,
                IncomingRequests = req.Count(r => r.ReceiverId == userId),
                OutgoingRequests = req.Count(r => r.SenderId == userId),
                MySkills = mySkills,
                RecommendedSkills = new List<UserSkill>() // AI LATER
            };

            return View(model);
        }
    }
}