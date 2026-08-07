using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class SkillController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private async Task<User?> GetAuthenticatedUserAsync()
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return null;
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(item =>
                        item.UserId == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return null;
            }

            if (user.IsBlocked ||
                user.IsDeactivated)
            {
                HttpContext.Session.Clear();
                return null;
            }

            return user;
        }

        // =====================================================
        // EXPLORE ACTIVE OFFERED SKILLS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var skills =
                await _context.UserSkills
                    .AsNoTracking()
                    .Include(skill => skill.User)
                    .Where(skill =>
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.User != null &&
                        !skill.User.IsBlocked &&
                        !skill.User.IsDeactivated)
                    .OrderByDescending(skill =>
                        skill.SkillId)
                    .ToListAsync();

            return View(skills);
        }

        // =====================================================
        // SEARCH ACTIVE OFFERED SKILLS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Search(
            string? searchTerm)
        {
            string cleanedSearch =
                searchTerm?.Trim() ?? string.Empty;

            var query =
                _context.UserSkills
                    .AsNoTracking()
                    .Include(skill => skill.User)
                    .Where(skill =>
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.User != null &&
                        !skill.User.IsBlocked &&
                        !skill.User.IsDeactivated);

            if (!string.IsNullOrWhiteSpace(
                    cleanedSearch))
            {
                query =
                    query.Where(skill =>
                        skill.SkillName.Contains(
                            cleanedSearch
                        )
                        ||
                        (
                            skill.Description != null &&
                            skill.Description.Contains(
                                cleanedSearch
                            )
                        )
                        ||
                        (
                            skill.User != null &&
                            skill.User.Username.Contains(
                                cleanedSearch
                            )
                        ));
            }

            var skills =
                await query
                    .OrderByDescending(skill =>
                        skill.SkillId)
                    .ToListAsync();

            ViewBag.SearchTerm =
                cleanedSearch;

            return View(
                "Index",
                skills
            );
        }

        // =====================================================
        // CREATE SKILL GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!user.IsEmailVerified)
            {
                TempData["Error"] =
                    "Please verify your email before adding skills.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            return View(
                new AddSkillViewModel()
            );
        }

        // =====================================================
        // CREATE OFFERED + WANTED SKILL
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AddSkillViewModel model,
            string? returnUrl = null)
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!currentUser.IsEmailVerified)
            {
                TempData["Error"] =
                    "Please verify your email before publishing a skill swap entry.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            string offeredSkillName =
                model.OfferedSkillName?.Trim()
                ?? string.Empty;

            string wantedSkillName =
                model.WantedSkillName?.Trim()
                ?? string.Empty;

            string description =
                model.Description?.Trim()
                ?? string.Empty;

            // =================================================
            // VALIDATION
            // =================================================

            if (string.IsNullOrWhiteSpace(
                    offeredSkillName))
            {
                ModelState.AddModelError(
                    nameof(model.OfferedSkillName),
                    "Offered skill name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    wantedSkillName))
            {
                ModelState.AddModelError(
                    nameof(model.WantedSkillName),
                    "Wanted skill name is required."
                );
            }

            if (model.ProficiencyLevel < 1 ||
                model.ProficiencyLevel > 5)
            {
                ModelState.AddModelError(
                    nameof(model.ProficiencyLevel),
                    "Proficiency level must be between 1 and 5."
                );
            }

            if (model.DesiredLevel < 1 ||
                model.DesiredLevel > 5)
            {
                ModelState.AddModelError(
                    nameof(model.DesiredLevel),
                    "Desired level must be between 1 and 5."
                );
            }

            if (!ModelState.IsValid)
            {
                return HandleInvalidCreateModel(
                    model,
                    returnUrl
                );
            }

            int userId =
                currentUser.UserId;

            // =================================================
            // DUPLICATE ACTIVE OFFERED SKILL CHECK
            // =================================================

            bool offeredSkillExists =
                await _context.UserSkills
                    .AnyAsync(skill =>
                        skill.UserId == userId &&
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.SkillName.ToLower() ==
                        offeredSkillName.ToLower());

            if (offeredSkillExists)
            {
                ModelState.AddModelError(
                    nameof(model.OfferedSkillName),
                    "You have already added this teaching skill."
                );
            }

            // =================================================
            // DUPLICATE ACTIVE WANTED SKILL CHECK
            // =================================================

            bool wantedSkillExists =
                await _context.UserWantedSkills
                    .AnyAsync(skill =>
                        skill.UserId == userId &&
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.SkillName.ToLower() ==
                        wantedSkillName.ToLower());

            if (wantedSkillExists)
            {
                ModelState.AddModelError(
                    nameof(model.WantedSkillName),
                    "You have already added this learning goal."
                );
            }

            if (!ModelState.IsValid)
            {
                return HandleInvalidCreateModel(
                    model,
                    returnUrl
                );
            }

            // =================================================
            // CREATE OFFERED SKILL
            // =================================================

            var userSkill =
                new UserSkill
                {
                    SkillName =
                        offeredSkillName,

                    ProficiencyLevel =
                        model.ProficiencyLevel,

                    Description =
                        description,

                    UserId =
                        userId,

                    IsActive =
                        true,

                    IsDeleted =
                        false,

                    AdminNote =
                        null
                };

            // =================================================
            // CREATE WANTED SKILL
            // =================================================

            var userWantedSkill =
                new UserWantedSkill
                {
                    SkillName =
                        wantedSkillName,

                    DesiredLevel =
                        model.DesiredLevel,

                    UserId =
                        userId,

                    CreatedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true,

                    IsDeleted =
                        false,

                    AdminNote =
                        null
                };

            _context.UserSkills.Add(
                userSkill
            );

            _context.UserWantedSkills.Add(
                userWantedSkill
            );

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill swap entry published successfully.";

            if (string.Equals(
                    returnUrl,
                    "MyAccount",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            return RedirectToAction(
                "Dashboard",
                "Home"
            );
        }

        // =====================================================
        // USER SOFT DELETE OFFERED SKILL
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var skill =
                await _context.UserSkills
                    .FirstOrDefaultAsync(item =>
                        item.SkillId == id &&
                        item.UserId == user.UserId &&
                        !item.IsDeleted);

            if (skill == null)
            {
                TempData["Error"] =
                    "Teaching skill was not found.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            skill.IsActive =
                false;

            skill.IsDeleted =
                true;

            skill.AdminNote =
                "Removed by the skill owner.";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Teaching skill removed successfully.";

            return RedirectToAction(
                "MyAccount",
                "Account"
            );
        }

        // =====================================================
        // USER SOFT DELETE WANTED SKILL
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWantedSkill(
            int id)
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var wantedSkill =
                await _context.UserWantedSkills
                    .FirstOrDefaultAsync(item =>
                        item.WantedSkillId == id &&
                        item.UserId == user.UserId &&
                        !item.IsDeleted);

            if (wantedSkill == null)
            {
                TempData["Error"] =
                    "Learning goal was not found.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            wantedSkill.IsActive =
                false;

            wantedSkill.IsDeleted =
                true;

            wantedSkill.AdminNote =
                "Removed by the skill owner.";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Learning goal removed successfully.";

            return RedirectToAction(
                "MyAccount",
                "Account"
            );
        }

        // =====================================================
        // INVALID CREATE MODEL HELPER
        // =====================================================

        private IActionResult HandleInvalidCreateModel(
            AddSkillViewModel model,
            string? returnUrl)
        {
            if (string.Equals(
                    returnUrl,
                    "MyAccount",
                    StringComparison.OrdinalIgnoreCase))
            {
                string errorMessage =
                    ModelState.Values
                        .SelectMany(value =>
                            value.Errors)
                        .Select(error =>
                            error.ErrorMessage)
                        .FirstOrDefault()
                    ?? "Please enter valid skill information.";

                TempData["Error"] =
                    errorMessage;

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            return View(
                "Create",
                model
            );
        }
    }
}