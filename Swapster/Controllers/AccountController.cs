using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Swapster.Data;
using Swapster.Models;
using Swapster.Services;
using System;
using System.Linq;

namespace Swapster.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _emailService = emailService;
        }

        private void SetUserSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username ?? "User");
            HttpContext.Session.SetString("Email", user.Email ?? "Not available");
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);
        }

        private User GetAuthenticatedUser()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;

            return _context.Users.FirstOrDefault(x => x.UserId == userId.Value);
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter email and password";
                return View();
            }

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

            SetUserSession(user);
            return RedirectToAction("Dashboard", "Home");
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(string email, string password)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter admin email and password";
                return View();
            }

            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid admin email or password";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid admin email or password";
                return View();
            }

            if (user.AccessLevel != 1)
            {
                ViewBag.Error = "You are not authorized as admin";
                return View();
            }

            SetUserSession(user);
            return RedirectToAction("AdminDashboard", "Home");
        }

        [HttpGet]
        public IActionResult Signup()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";
            return View();
        }

        [HttpPost]
        public IActionResult Signup(User user, string password, string confirmPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (user == null)
            {
                ViewBag.Error = "Invalid signup data";
                return View();
            }

            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please fill all required fields";
                return View(user);
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Password and confirm password do not match";
                return View(user);
            }

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

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email";
                return View();
            }

            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Email not found";
                return View();
            }

            string token = Guid.NewGuid().ToString();
            user.VerificationToken = token;
            user.TokenExpiry = DateTime.Now.AddMinutes(15);
            _context.SaveChanges();

            string resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

            string subject = "Reset Your Swapster Password";
            string body = $@"
                <h2>Reset Your Password</h2>
                <p>Hello {user.Username},</p>
                <p>You requested to reset your Swapster password.</p>
                <p>Click the button below to reset your password:</p>
                <p>
                    <a href='{resetLink}' style='background:#6674ff;color:white;padding:12px 18px;text-decoration:none;border-radius:6px;display:inline-block;'>
                        Reset Password
                    </a>
                </p>
                <p>Or copy this link and open it in your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 15 minutes.</p>
                <br />
                <p>Regards,<br/>Swapster Team</p>
            ";

            bool emailSent = _emailService.SendEmail(email, subject, body);

            if (!emailSent)
            {
                ViewBag.Error = "Email send nahi ho saki. Gmail app password ya SMTP settings check karo.";
                return View();
            }

            ViewBag.Message = "Reset link sent to your email.";
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            var user = GetAuthenticatedUser();
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(token))
            {
                return Content("Invalid or expired token");
            }

            var user = _context.Users.FirstOrDefault(x => x.VerificationToken == token && x.TokenExpiry > DateTime.Now);

            if (user == null)
            {
                return Content("Invalid or expired token");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Content("Invalid request");
            }

            var user = _context.Users.FirstOrDefault(x => x.VerificationToken == token && x.TokenExpiry > DateTime.Now);

            if (user == null)
            {
                return Content("Invalid or expired token");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.VerificationToken = null;
            user.TokenExpiry = null;
            _context.SaveChanges();

            SetUserSession(user);
            TempData["msg"] = "Password reset successful!";

            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            var user = GetAuthenticatedUser();
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and Confirm password do not match.";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Incorrect current password.";
                return View();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _context.SaveChanges();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("MyAccount", "Account");
        }

        [HttpGet]
        public IActionResult MyAccount()
        {
            ViewData["HideNavbar"] = true;
            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "dashboard-page";

            var user = GetAuthenticatedUser();
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult UpdateAccount(string username, string email)
        {
            ViewData["HideNavbar"] = true;
            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "dashboard-page";

            var user = GetAuthenticatedUser();
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Username and email are required.";
                return RedirectToAction("MyAccount", "Account");
            }

            bool emailExists = _context.Users.Any(x => x.Email == email && x.UserId != user.UserId);

            if (emailExists)
            {
                TempData["Error"] = "This email is already used by another account.";
                return RedirectToAction("MyAccount", "Account");
            }

            user.Username = username.Trim();
            user.Email = email.Trim();
            _context.SaveChanges();

            SetUserSession(user);

            TempData["Success"] = "Account updated successfully.";
            return RedirectToAction("MyAccount", "Account");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}