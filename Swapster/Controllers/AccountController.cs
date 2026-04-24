using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Swapster.Data;
using Swapster.Models;
using Swapster.Services;

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

        // ================= LOGIN GET =================
        public IActionResult Login()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        // ================= LOGIN POST =================
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

            return RedirectToAction("Dashboard", "Home");
        }

        // ================= ADMIN LOGIN GET =================
        public IActionResult AdminLogin()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        // ================= ADMIN LOGIN POST =================
        [HttpPost]
        public IActionResult AdminLogin(string email, string password)
        {
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

            // Admin AccessLevel = 1
            if (user.AccessLevel != 1)
            {
                ViewBag.Error = "You are not authorized as admin";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);

            // Agar AdminController nahi bana hua to Home Dashboard par bhej do
            return RedirectToAction("Dashboard", "Home");

            // Agar baad mein AdminController banao, tab ye use karna:
            // return RedirectToAction("Dashboard", "Admin");
        }

        // ================= SIGNUP GET =================
        public IActionResult Signup()
        {
            ViewData["HideNavbar"] = true;
            return View();
        }

        // ================= SIGNUP POST =================
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

        // ================= FORGOT PASSWORD GET =================
        public IActionResult ForgotPassword()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";
            return View();
        }

        // ================= FORGOT PASSWORD POST =================
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

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

            string resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token },
                Request.Scheme
            );

            string subject = "Reset Your Swapster Password";

            string body = $@"
                <h2>Reset Your Password</h2>

                <p>Hello {user.Username},</p>

                <p>You requested to reset your Swapster password.</p>

                <p>Click the button below to reset your password:</p>

                <p>
                    <a href='{resetLink}'
                       style='background:#007bff;color:white;padding:12px 18px;text-decoration:none;border-radius:6px;display:inline-block;'>
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

        // ================= RESET PASSWORD GET =================
        public IActionResult ResetPassword(string token)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

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

        // ================= RESET PASSWORD POST =================
        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            var user = _context.Users.FirstOrDefault(x =>
                x.VerificationToken == token &&
                x.TokenExpiry > DateTime.Now);

            if (user == null)
            {
                return Content("Invalid or expired token");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            user.VerificationToken = null;
            user.TokenExpiry = null;

            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("AccessLevel", user.AccessLevel);

            TempData["msg"] = "Password reset successful! You are now logged in.";

            return RedirectToAction("Dashboard", "Home");
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}