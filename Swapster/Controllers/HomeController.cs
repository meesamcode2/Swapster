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
using Microsoft.AspNetCore.Http;

namespace Swapster.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            string username = HttpContext.Session.GetString("Username") ?? "User";
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            List<UserSkill> recommendedSkillsList = new List<UserSkill>();

            if (currentUserId.HasValue)
            {
                var currentUserWants = await _context.UserWantedSkills
                    .Where(w => w.UserId == currentUserId.Value)
                    .ToListAsync();

                if (currentUserWants.Any())
                {
                    var allOtherSkills = await _context.UserSkills
                        .Include(s => s.User)
                        .Where(s => s.UserId != currentUserId.Value)
                        .ToListAsync();

                    var scoredSkills = allOtherSkills.Select(skill =>
                    {
                        var matchingWant = currentUserWants
                            .FirstOrDefault(w => w.SkillName.Equals(skill.SkillName, StringComparison.OrdinalIgnoreCase));

                        double score = 0;
                        if (matchingWant != null)
                        {
                            double diff = matchingWant.DesiredLevel - skill.ProficiencyLevel;
                            double sumOfSquares = Math.Pow(diff, 2);
                            score = 1.0 / (1.0 + sumOfSquares);
                        }

                        return new { Skill = skill, MatchScore = score };
                    })
                    .Where(x => x.MatchScore > 0)
                    .OrderByDescending(x => x.MatchScore)
                    .Select(x => x.Skill)
                    .Take(6)
                    .ToList();

                    recommendedSkillsList = scoredSkills;
                }
            }

            if (!recommendedSkillsList.Any())
            {
                recommendedSkillsList = await _context.UserSkills
                    .Include(s => s.User)
                    .OrderByDescending(s => s.SkillId)
                    .Take(6)
                    .ToListAsync();
            }

            var model = new DashboardViewModel
            {
                UserName = username,
                RecommendedSkills = recommendedSkillsList
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            int? accessLevel = HttpContext.Session.GetInt32("AccessLevel");
            if (accessLevel != 1)
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            var model = new AdminDashboardViewModel
            {
                AdminName = HttpContext.Session.GetString("Username") ?? "Administrator",
                TotalUsers = _context.Users.Count(),
                ActiveSwaps = 42,
                TotalSkills = _context.UserSkills.Count(),
                PendingReportsCount = 0
            };

            return View("AdminDashboardViewModel", model);
        }

        public IActionResult Settings()
        {
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "User";
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "Not available";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { answer = "Please type a message first." });
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var payload = new { message = message };
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://127.0.0.1:5000/chat", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { answer = "Swapster is having connection issue." });
                    }

                    var result = await response.Content.ReadAsStringAsync();
                    return Content(result, "application/json");
                }
            }
            catch
            {
                return Json(new { answer = "Swapster is having connection issue: http://127.0.0.1:5000/chat" });
            }
        }
    }
}