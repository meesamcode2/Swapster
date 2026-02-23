using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class SkillController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all skills
        public IActionResult Index()
        {
            var skills = _context.UserSkills.Include(s => s.User).ToList();
            return View(skills);
        }

        // GET Create
        public IActionResult Create()
        {
            return View();
        }

        // POST Create
        [HttpPost]
        public IActionResult Create(UserSkill skill)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            skill.UserId = userId.Value;

            _context.UserSkills.Add(skill);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}