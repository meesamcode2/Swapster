using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System.Text;

namespace Swapster.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LANDING PAGE =================
        public IActionResult Index()
        {
            return View();
        }

        // ================= DASHBOARD PAGE =================
        public async Task<IActionResult> Dashboard()
        {
            string username = HttpContext.Session.GetString("Username") ?? "User";

            var model = new DashboardViewModel
            {
                UserName = username,

                RecommendedSkills = await _context.UserSkills
                    .Include(s => s.User)
                    .OrderByDescending(s => s.SkillId)
                    .Take(6)
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Settings()
        {
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "User";
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "Not available";

            return View();
        }

        // ================= PRIVACY PAGE =================
        public IActionResult Privacy()
        {
            return View();
        }

        // ================= TERMS PAGE =================
        public IActionResult Terms()
        {
            return View();
        }

        // ================= CONTACT PAGE =================
        public IActionResult Contact()
        {
            return View();
        }

        // ================= CHATBOT API =================
        [HttpPost]
        public async Task<IActionResult> Chat(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new
                {
                    answer = "Please type a message first."
                });
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var payload = new { message = message };

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);

                    var content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync("http://127.0.0.1:5000/chat", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            answer = "Swapster se response nahi aa raha. Flask server check karo."
                        });
                    }

                    var result = await response.Content.ReadAsStringAsync();

                    return Content(result, "application/json");
                }
            }
            catch
            {
                return Json(new
                {
                    answer = "Swapster se connect nahi ho pa raha. Flask API run karo: http://127.0.0.1:5000/chat"
                });
            }
        }
    }
}