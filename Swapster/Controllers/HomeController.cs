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

            // =================================================
            // PERSONALIZED SKILL RECOMMENDATIONS
            // =================================================

            if (currentUserId.HasValue)
            {
                // Only active and non-deleted wanted skills
                var currentUserWants =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .Where(wanted =>
                            wanted.UserId == currentUserId.Value &&
                            wanted.IsActive &&
                            !wanted.IsDeleted)
                        .ToListAsync();

                if (currentUserWants.Any())
                {
                    // Only active and non-deleted offered skills
                    // belonging to other users
                    var allOtherSkills =
                        await _context.UserSkills
                            .AsNoTracking()
                            .Include(skill => skill.User)
                            .Where(skill =>
                                skill.UserId != currentUserId.Value &&
                                skill.IsActive &&
                                !skill.IsDeleted)
                            .ToListAsync();

                    var scoredSkills =
                        allOtherSkills
                            .Select(skill =>
                            {
                                var matchingWant =
                                    currentUserWants.FirstOrDefault(
                                        wanted =>
                                            string.Equals(
                                                wanted.SkillName?.Trim(),
                                                skill.SkillName?.Trim(),
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                    );

                                double score = 0;

                                if (matchingWant != null)
                                {
                                    double difference =
                                        matchingWant.DesiredLevel -
                                        skill.ProficiencyLevel;

                                    double sumOfSquares =
                                        Math.Pow(difference, 2);

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
                                result.Skill.ProficiencyLevel)
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
                        .Include(skill => skill.User)
                        .Where(skill =>
                            skill.IsActive &&
                            !skill.IsDeleted &&
                            (
                                !currentUserId.HasValue ||
                                skill.UserId != currentUserId.Value
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
                    .Include(post => post.User)
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
                HttpContext.Session.GetInt32("AccessLevel");

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

                    // Only non-deleted skills are counted
                    TotalSkills =
                        _context.UserSkills.Count(skill =>
                            !skill.IsDeleted),

                    ActiveSwaps =
                        _context.SkillRequests.Count(request =>
                            request.Status == "Accepted"),

                    CompletedSwaps =
                        _context.SkillRequests.Count(request =>
                            request.Status == "Completed"),

                    PendingRequests =
                        _context.SkillRequests.Count(request =>
                            request.Status == "Pending")
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
        public IActionResult Settings()
        {
            ViewBag.Username =
                HttpContext.Session.GetString("Username")
                ?? "User";

            ViewBag.Email =
                HttpContext.Session.GetString("Email")
                ?? "Not available";

            return View();
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
            if (string.IsNullOrWhiteSpace(message))
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
                        message = message.Trim()
                    };

                string json =
                    System.Text.Json.JsonSerializer
                        .Serialize(payload);

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
                            "Swapster is having a connection issue"
                    }
                );
            }
        }
    }
}