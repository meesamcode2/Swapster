using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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

        // ================= LOGIN =================
        public IActionResult Login()
        {
            ViewData["HideNavbar"] = true;
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

        // ================= SIGNUP =================
        public IActionResult Signup()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult Signup(User user, string password)
        {
            if (_context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            user.AccessLevel = 2;
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.IsEmailVerified = true;

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ================= FORGOT PASSWORD (GET) =================
        public IActionResult ForgotPassword()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        // ================= FORGOT PASSWORD (POST) =================
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Message = "Email not found";
                return View();
            }

            string token = Guid.NewGuid().ToString();

            user.VerificationToken = token;
            user.TokenExpiry = DateTime.Now.AddMinutes(15);

            _context.SaveChanges();

            string resetLink = Url.Action("ResetPassword", "Account",
                new { token = token }, Request.Scheme);

            // 🔥 EMAIL SERVICE CALL (uncomment when ready)
            // _emailService.SendEmail(email, "Reset Password", resetLink);

            ViewBag.Message = "Reset link sent to your email";
            return View();
        }

        // ================= RESET PASSWORD (GET) =================
        public IActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.VerificationToken == token &&
                x.TokenExpiry > DateTime.Now);

            if (user == null)
            {
                return Content("Invalid or expired token");
            }

            ViewBag.Token = token;
            return View();
        }

        // ================= RESET PASSWORD (POST) =================
        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.VerificationToken == token &&
                x.TokenExpiry > DateTime.Now);

            if (user == null)
            {
                return Content("Invalid or expired token");
            }

            // 🔐 Update password
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            // 🧹 Clear token
            user.VerificationToken = null;
            user.TokenExpiry = null;

            _context.SaveChanges();

            // 🚀 AUTO LOGIN AFTER RESET
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);

            // optional message
            TempData["msg"] = "Password reset successful! You are now logged in.";

            // redirect to home directly
            return RedirectToAction("Index", "Home");
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}