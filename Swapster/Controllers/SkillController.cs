using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Swapster.Controllers
{
    public class SkillController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        public async Task<IActionResult> Index()
        {
            var skills = await _context.UserSkills.Include(s => s.User).ToListAsync();
            return View(skills);
        }

        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            return View(new AddSkillViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddSkillViewModel model)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userSkill = new UserSkill
            {
                SkillName = model.OfferedSkillName,
                ProficiencyLevel = model.ProficiencyLevel,
                Description = model.Description ?? string.Empty,
                UserId = userId.Value
            };

            var userWantedSkill = new UserWantedSkill
            {
                SkillName = model.WantedSkillName,
                DesiredLevel = model.DesiredLevel,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSkills.Add(userSkill);
            _context.UserWantedSkills.Add(userWantedSkill);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Home");
        }
    }
}