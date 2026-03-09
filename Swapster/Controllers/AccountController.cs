using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }


        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);
            return RedirectToAction("Index", "Home");
        }


        public IActionResult Signup()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Signup(User user, string password)
        {
            if (!ModelState.IsValid)
                return View(user);
            if (_context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }


        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}