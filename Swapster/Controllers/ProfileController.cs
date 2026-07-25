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

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            
            var existingProfile = _context.UserProfiles
        .FirstOrDefault(p => p.UserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        
        [HttpPost]
        public IActionResult Setup(UserProfile profile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

           
            var existingProfile = _context.UserProfiles
        .FirstOrDefault(p => p.UserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction("Index", "Home");
            }

            profile.UserId = userId.Value;

            _context.UserProfiles.Add(profile);
            _context.SaveChanges();

           
            return RedirectToAction("Index", "Home");
        }
    }
}