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

        // List all offered skills
        public IActionResult Index()
        {
            var skills = _context.UserSkills.Include(s => s.User).ToList();
            return View(skills);
        }

        // GET: Skill/Create
        public IActionResult Create()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Naya ViewModel initialize kar ke view ko bhej rahe hain
            return View(new AddSkillViewModel());
        }

        // POST: Skill/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddSkillViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // 1. Mapping ViewModel Data to UserSkill (Offer)
                var userSkill = new UserSkill
                {
                    SkillName = model.OfferedSkillName,
                    ProficiencyLevel = model.ProficiencyLevel,
                    Description = model.Description ?? string.Empty,
                    UserId = userId.Value
                };
                _context.UserSkills.Add(userSkill);

                // 2. Mapping ViewModel Data to UserWantedSkill (Want)
                var userWantedSkill = new UserWantedSkill
                {
                    SkillName = model.WantedSkillName,
                    DesiredLevel = model.DesiredLevel,
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserWantedSkills.Add(userWantedSkill);

                // 3. Save both entries in a single transaction
                await _context.SaveChangesAsync();

                // Dashboard ya Index page par redirect karein jahan aap chahein
                return RedirectToAction("Dashboard", "Home");
            }

            // Agar koi validation fail ho jaye toh form values ke sath wapas jaye
            return View(model);
        }
    }
}