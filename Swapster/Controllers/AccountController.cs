using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using Swapster.Services;

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

        // ===================== LOGIN =====================
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

            // ✅ Session set
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);

            // check if profile exists
            var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.UserId);

            if (profile == null)
            {
                return RedirectToAction("Setup", "Profile");
            }

            return RedirectToAction("Index", "Home");
        }

        // ===================== ADMIN LOGIN =====================
        public IActionResult AdminLogin()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            // ❌ Only admin allowed
            if (user.AccessLevel != 0)
            {
                ViewBag.Error = "You are not an admin";
                return View();
            }
                     

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            // ✅ Session set
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);

            return RedirectToAction("Index", "Home");
        }

        // ===================== SIGNUP =====================
        public IActionResult Signup()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult Signup(User user, string password, [FromServices] EmailService emailService)
        {
            if (!ModelState.IsValid)
                return View(user);

            if (_context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email already registered";
                return View(user);
            }

            // ✅ Default role = Guest
            user.AccessLevel = 2;

            // ✅ Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            // ✅ Email verification
            user.VerificationToken = Guid.NewGuid().ToString();

            _context.Users.Add(user);
            _context.SaveChanges();

            string verifyLink = Url.Action("VerifyEmail", "Account",
                new { token = user.VerificationToken }, Request.Scheme);

            emailService.SendEmail(
                user.Email,
                "Verify your Swapster Email",
                $"<h3>Click below to verify:</h3><a href='{verifyLink}'>Verify Email</a>"
            );

            TempData["msg"] = "Signup successful! Check your email to verify.";
            return RedirectToAction("Login");
        }

        // ===================== EMAIL VERIFY =====================
        public IActionResult VerifyEmail(string token)
        {
            var user = _context.Users.FirstOrDefault(x => x.VerificationToken == token);

            if (user == null)
                return Content("Invalid verification link");

            user.IsEmailVerified = true;
            user.VerificationToken = null;
            _context.SaveChanges();

            return Content("Email verified! You can now log in.");
        }

        // ===================== FORGOT PASSWORD =====================
        public IActionResult ForgotPassword()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email, [FromServices] EmailService emailService)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Email not found";
                return View();
            }

            user.ResetPasswordToken = Guid.NewGuid().ToString();
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);
            _context.SaveChanges();

            string resetLink = Url.Action("ResetPassword", "Account",
                new { token = user.ResetPasswordToken }, Request.Scheme);

            emailService.SendEmail(
                user.Email,
                "Reset Your Password",
                $"Click to reset your password:<br/><a href='{resetLink}'>Reset Password</a>"
            );

            ViewBag.Message = "Password reset link sent!";
            return View();
        }

        // ===================== RESET PASSWORD =====================
        public IActionResult ResetPassword(string token)
        {
            ViewData["HideNavbar"] = true;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.ResetPasswordToken == token &&
                x.ResetPasswordExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Error = "Invalid or expired link";
                return View();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            _context.SaveChanges();

            return Content("Password reset successful.");
        }

        // ===================== LOGOUT =====================
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}