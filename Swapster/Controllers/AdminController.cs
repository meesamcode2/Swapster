using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            int? accessLevel = HttpContext.Session.GetInt32("AccessLevel");
            return accessLevel == 1;
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            var users = await _context.Users
                .OrderBy(u => u.UserId)
                .ToListAsync();

            return View(users);
        }
        public async Task<IActionResult> Skills()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            var offeredSkills = await _context.UserSkills
                .Include(s => s.User)
                .OrderByDescending(s => s.SkillId)
                .ToListAsync();

            var wantedSkills = await _context.UserWantedSkills
                .Include(s => s.User)
                .OrderByDescending(s => s.WantedSkillId)
                .ToListAsync();

            var model = new Models.AdminSkillsViewModel
            {
                OfferedSkills = offeredSkills,
                WantedSkills = wantedSkills
            };

            return View(model);
        }
        public async Task<IActionResult> Exchanges()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            var exchanges = await _context.SkillRequests
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Include(r => r.Skill)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(exchanges);
        }
    }
}