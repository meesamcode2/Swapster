using Microsoft.AspNetCore.Mvc;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Setup()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult Setup(UserProfile profile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            profile.UserId = userId.Value;

            _context.UserProfiles.Add(profile);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}