using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LANDING PAGE
        // =====================================================

        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // USER DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            string username =
                HttpContext.Session.GetString("Username")
                ?? "User";

            int? currentUserId =
                HttpContext.Session.GetInt32("UserId");

            var recommendedSkillsList =
                new List<UserSkill>();

            if (currentUserId.HasValue)
            {
                var currentUser =
                    await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(user =>
                            user.UserId ==
                            currentUserId.Value);

                if (currentUser == null ||
                    currentUser.IsBlocked ||
                    currentUser.IsDeactivated)
                {
                    HttpContext.Session.Clear();

                    TempData["Error"] =
                        "Please sign in to continue.";

                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }

                username =
                    currentUser.Username ?? username;

                // =============================================
                // PERSONALIZED SKILL RECOMMENDATIONS
                // =============================================

                var currentUserWants =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .Where(wanted =>
                            wanted.UserId ==
                                currentUserId.Value &&
                            wanted.IsActive &&
                            !wanted.IsDeleted)
                        .ToListAsync();

                if (currentUserWants.Any())
                {
                    var allOtherSkills =
                        await _context.UserSkills
                            .AsNoTracking()
                            .Include(skill =>
                                skill.User)
                            .Where(skill =>
                                skill.UserId !=
                                    currentUserId.Value &&
                                skill.IsActive &&
                                !skill.IsDeleted &&
                                skill.User != null &&
                                !skill.User.IsBlocked &&
                                !skill.User.IsDeactivated)
                            .ToListAsync();

                    var scoredSkills =
                        allOtherSkills
                            .Select(skill =>
                            {
                                var matchingWant =
                                    currentUserWants
                                        .FirstOrDefault(
                                            wanted =>
                                                string.Equals(
                                                    wanted.SkillName
                                                        ?.Trim(),
                                                    skill.SkillName
                                                        ?.Trim(),
                                                    StringComparison
                                                        .OrdinalIgnoreCase
                                                )
                                        );

                                double score = 0;

                                if (matchingWant != null)
                                {
                                    double difference =
                                        matchingWant.DesiredLevel -
                                        skill.ProficiencyLevel;

                                    double sumOfSquares =
                                        Math.Pow(
                                            difference,
                                            2
                                        );

                                    score =
                                        1.0 /
                                        (1.0 + sumOfSquares);
                                }

                                return new
                                {
                                    Skill = skill,
                                    MatchScore = score
                                };
                            })
                            .Where(result =>
                                result.MatchScore > 0)
                            .OrderByDescending(result =>
                                result.MatchScore)
                            .ThenByDescending(result =>
                                result.Skill
                                    .ProficiencyLevel)
                            .Select(result =>
                                result.Skill)
                            .Take(6)
                            .ToList();

                    recommendedSkillsList =
                        scoredSkills;
                }
            }

            // =================================================
            // FALLBACK RECOMMENDATIONS
            // =================================================

            if (!recommendedSkillsList.Any())
            {
                recommendedSkillsList =
                    await _context.UserSkills
                        .AsNoTracking()
                        .Include(skill =>
                            skill.User)
                        .Where(skill =>
                            skill.IsActive &&
                            !skill.IsDeleted &&
                            skill.User != null &&
                            !skill.User.IsBlocked &&
                            !skill.User.IsDeactivated &&
                            (
                                !currentUserId.HasValue ||
                                skill.UserId !=
                                    currentUserId.Value
                            ))
                        .OrderByDescending(skill =>
                            skill.SkillId)
                        .Take(6)
                        .ToListAsync();
            }

            // =================================================
            // COMMUNITY POSTS
            // =================================================

            var communityPosts =
                await _context.Posts
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(post =>
                        post.User)
                    .Include(post =>
                        post.Likes)
                    .Include(post =>
                        post.Comments)
                        .ThenInclude(comment =>
                            comment.User)
                    .Include(post =>
                        post.Shares)
                    .Where(post =>
                        post.User != null &&
                        !post.User.IsBlocked &&
                        !post.User.IsDeactivated)
                    .OrderByDescending(post =>
                        post.CreatedAt)
                    .Take(15)
                    .ToListAsync();

            // =================================================
            // DASHBOARD VIEW MODEL
            // =================================================

            var model =
                new DashboardViewModel
                {
                    UserName =
                        username,

                    RecommendedSkills =
                        recommendedSkillsList,

                    CommunityPosts =
                        communityPosts
                };

            return View(model);
        }

        // =====================================================
        // ADMIN DASHBOARD
        // =====================================================

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            int? accessLevel =
                HttpContext.Session.GetInt32(
                    "AccessLevel"
                );

            if (accessLevel != 1)
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            var model =
                new AdminDashboardViewModel
                {
                    AdminName =
                        HttpContext.Session
                            .GetString("Username")
                        ?? "Administrator",

                    TotalUsers =
                        _context.Users.Count(),

                    TotalSkills =
                        _context.UserSkills.Count(
                            skill =>
                                !skill.IsDeleted
                        ),

                    ActiveSwaps =
                        _context.SkillRequests.Count(
                            request =>
                                request.Status ==
                                "Accepted"
                        ),

                    CompletedSwaps =
                        _context.SkillRequests.Count(
                            request =>
                                request.Status ==
                                "Completed"
                        ),

                    PendingRequests =
                        _context.SkillRequests.Count(
                            request =>
                                request.Status ==
                                "Pending"
                        )
                };

            return View(
                "AdminDashboardViewModel",
                model
            );
        }

        // =====================================================
        // SETTINGS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    "UserId"
                );

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.UserId ==
                            userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (user.IsBlocked)
            {
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your account has been blocked by the administrator.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (user.IsDeactivated)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            ViewBag.Username =
                user.Username ?? "User";

            ViewBag.Email =
                user.Email ?? "Not available";

            ViewBag.IsEmailVerified =
                user.IsEmailVerified;

            return View();
        }

        // =====================================================
        // DEACTIVATE ACCOUNT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            DeactivateAccount()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    "UserId"
                );

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(item =>
                        item.UserId ==
                        userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (user.AccessLevel == 1)
            {
                TempData["Error"] =
                    "Administrator accounts cannot be self-deactivated.";

                return RedirectToAction(
                    nameof(Settings)
                );
            }

            if (user.IsBlocked)
            {
                HttpContext.Session.Clear();

                TempData["Error"] =
                    "Your account has been blocked by the administrator.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Mark the account as deactivated and blocked.
            // This makes it appear in the existing Admin user-management
            // blocked list, where an administrator can reactivate it.
            user.IsDeactivated =
                true;

            user.DeactivatedAt =
                DateTime.UtcNow;

            user.IsBlocked =
                true;

            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();

            TempData["Success"] =
                "Your account has been deactivated. An administrator must reactivate it before you can sign in again.";

            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        // =====================================================
        // STATIC PAGES
        // =====================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // =====================================================
        // AI CHATBOT
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Chat(
            string message)
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    "UserId"
                );

            if (userId == null)
            {
                return Json(
                    new
                    {
                        answer =
                            "Please sign in to use Swapster AI."
                    }
                );
            }

            var user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.UserId ==
                        userId.Value);

            if (user == null ||
                user.IsBlocked ||
                user.IsDeactivated)
            {
                HttpContext.Session.Clear();

                return Json(
                    new
                    {
                        answer =
                            "Your session is no longer active. Please sign in again."
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(
                    message
                ))
            {
                return Json(
                    new
                    {
                        answer =
                            "Please type a message first."
                    }
                );
            }

            try
            {
                using var client =
                    new HttpClient();

                var payload =
                    new
                    {
                        message =
                            message.Trim()
                    };

                string json =
                    JsonSerializer.Serialize(
                        payload
                    );

                using var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                var response =
                    await client.PostAsync(
                        "http://127.0.0.1:5000/chat",
                        content
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return Json(
                        new
                        {
                            answer =
                                "Swapster is having a connection issue."
                        }
                    );
                }

                string result =
                    await response.Content
                        .ReadAsStringAsync();

                return Content(
                    result,
                    "application/json"
                );
            }
            catch
            {
                return Json(
                    new
                    {
                        answer =
                            "Swapster is having a connection issue."
                    }
                );
            }
        }
    }
}