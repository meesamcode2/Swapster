using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using Swapster.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly EmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = new PasswordHasher<User>();
        }

        private bool SendVerificationEmail(User user)
        {
            string token =
                Guid.NewGuid().ToString("N");

            user.EmailVerificationToken =
                token;

            user.EmailVerificationTokenExpiry =
                DateTime.UtcNow.AddHours(24);

            _context.SaveChanges();

            string? verificationLink =
                Url.Action(
                    "VerifyEmail",
                    "Account",
                    new
                    {
                        token
                    },
                    Request.Scheme
                );

            if (string.IsNullOrWhiteSpace(verificationLink))
            {
                return false;
            }

            string subject =
                "Verify Your Swapster Email";

            string body = $@"
        <h2>Verify Your Email</h2>

        <p>Hello {user.Username},</p>

        <p>
            Thank you for creating your Swapster account.
            Please verify your email before publishing skills.
        </p>

        <p>
            <a href='{verificationLink}'
               style='background:#6674ff;
                      color:white;
                      padding:12px 18px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>

                Verify Email
            </a>
        </p>

        <p>
            Or copy and open this link:
        </p>

        <p>{verificationLink}</p>

        <p>
            This verification link will expire in 24 hours.
        </p>

        <br />

        <p>
            Regards,
            <br />
            Swapster Team
        </p>
    ";

            return _emailService.SendEmail(
                user.Email,
                subject,
                body
            );
        }

        // =====================================================
        // SESSION HELPERS
        // =====================================================

        private void SetUserSession(User user)
        {
            HttpContext.Session.SetInt32(
                "UserId",
                user.UserId
            );

            HttpContext.Session.SetString(
                "Username",
                user.Username ?? "User"
            );

            HttpContext.Session.SetString(
                "Email",
                user.Email ?? "Not available"
            );

            HttpContext.Session.SetInt32(
                "AccessLevel",
                user.AccessLevel
            );
        }

        private async Task<User?> GetAuthenticatedUserAsync()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return null;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return null;
            }

            // Blocked user ki existing session bhi close ho jayegi
            if (user.IsBlocked)
            {
                HttpContext.Session.Clear();
                return null;
            }

            return user;
        }

        // =====================================================
        // USER LOGIN
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(
            string email,
            string password)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            email = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please enter email and password.";

                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.Email == email);

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid email or password.";

                return View();
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password
                );

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ViewBag.Error =
                    "Invalid email or password.";

                return View();
            }

            // Blocked user login nahi kar sakta
            if (user.IsBlocked)
            {
                ViewBag.Error =
                    "Your account has been blocked by the administrator.";

                return View();
            }

            SetUserSession(user);

            return RedirectToAction(
                "Dashboard",
                "Home"
            );
        }

        // =====================================================
        // ADMIN LOGIN
        // =====================================================

        [HttpGet]
        public IActionResult AdminLogin()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminLogin(
            string email,
            string password)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            email = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please enter admin email and password.";

                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.Email == email);

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid admin email or password.";

                return View();
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password
                );

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ViewBag.Error =
                    "Invalid admin email or password.";

                return View();
            }

            if (user.IsBlocked)
            {
                ViewBag.Error =
                    "This administrator account has been blocked.";

                return View();
            }

            if (user.AccessLevel != 1)
            {
                ViewBag.Error =
                    "You are not authorized as administrator.";

                return View();
            }

            SetUserSession(user);

            return RedirectToAction(
                "AdminDashboard",
                "Home"
            );
        }

        // =====================================================
        // SIGNUP
        // =====================================================

        [HttpGet]
        public IActionResult Signup()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(
    User user,
    string password,
    string confirmPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid signup data.";

                return View();
            }

            user.Username =
                user.Username?.Trim() ?? string.Empty;

            user.Email =
                user.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please fill all required fields.";

                return View(user);
            }

            if (password != confirmPassword)
            {
                ViewBag.Error =
                    "Password and confirm password do not match.";

                return View(user);
            }

            bool emailExists =
                await _context.Users.AnyAsync(item =>
                    item.Email == user.Email);

            if (emailExists)
            {
                ViewBag.Error =
                    "Email already exists.";

                return View(user);
            }

            user.AccessLevel = 2;

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    password
                );

            // New user initially unverified hoga
            user.IsEmailVerified = false;

            user.IsBlocked = false;

            user.CreatedAt =
                DateTime.UtcNow;

            user.EmailVerificationToken =
                Guid.NewGuid().ToString("N");

            user.EmailVerificationTokenExpiry =
                DateTime.UtcNow.AddHours(24);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            string? verificationLink =
                Url.Action(
                    "VerifyEmail",
                    "Account",
                    new
                    {
                        token = user.EmailVerificationToken
                    },
                    Request.Scheme
                );

            string subject =
                "Verify Your Swapster Email";

            string body = $@"
        <h2>Verify Your Email</h2>

        <p>Hello {user.Username},</p>

        <p>
            Your Swapster account has been created.
            Please verify your email before adding skills.
        </p>

        <p>
            <a href='{verificationLink}'
               style='background:#6674ff;
                      color:white;
                      padding:12px 18px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>

                Verify Email
            </a>
        </p>

        <p>
            Or copy and open this link:
        </p>

        <p>{verificationLink}</p>

        <p>
            This verification link will expire in 24 hours.
        </p>

        <br />

        <p>
            Regards,
            <br />
            Swapster Team
        </p>
    ";

            bool emailSent =
                _emailService.SendEmail(
                    user.Email,
                    subject,
                    body
                );

            if (emailSent)
            {
                TempData["Success"] =
                    "Account created. A verification link has been sent to your email.";
            }
            else
            {
                TempData["Error"] =
                    "Account created, but verification email could not be sent. Log in and resend the verification email.";
            }

            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        // =====================================================
        // FORGOT PASSWORD
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(
    string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "Invalid email verification link.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.EmailVerificationToken == token &&
                    item.EmailVerificationTokenExpiry >
                    DateTime.UtcNow);

            if (user == null)
            {
                TempData["Error"] =
                    "The email verification link is invalid or has expired.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            user.IsEmailVerified = true;

            user.EmailVerificationToken = null;

            user.EmailVerificationTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Email verified successfully. You can now publish skills.";

            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (user.IsEmailVerified)
            {
                TempData["Success"] =
                    "Your email is already verified.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            user.EmailVerificationToken =
                Guid.NewGuid().ToString("N");

            user.EmailVerificationTokenExpiry =
                DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();

            string? verificationLink =
                Url.Action(
                    "VerifyEmail",
                    "Account",
                    new
                    {
                        token = user.EmailVerificationToken
                    },
                    Request.Scheme
                );

            string subject =
                "Verify Your Swapster Email";

            string body = $@"
        <h2>Verify Your Email</h2>

        <p>Hello {user.Username},</p>

        <p>
            Please verify your Swapster email before publishing skills.
        </p>

        <p>
            <a href='{verificationLink}'
               style='background:#6674ff;
                      color:white;
                      padding:12px 18px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>

                Verify Email
            </a>
        </p>

        <p>
            This link will expire in 24 hours.
        </p>
    ";

            bool emailSent =
                _emailService.SendEmail(
                    user.Email,
                    subject,
                    body
                );

            if (!emailSent)
            {
                TempData["Error"] =
                    "Verification email could not be sent. Check SMTP settings.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            TempData["Success"] =
                "A new verification link has been sent to your email.";

            return RedirectToAction(
                "MyAccount",
                "Account"
            );
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(
            string email)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            email = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error =
                    "Please enter your email.";

                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.Email == email);

            if (user == null)
            {
                ViewBag.Error =
                    "Email not found.";

                return View();
            }

            string token =
                Guid.NewGuid().ToString();

            user.VerificationToken = token;

            user.TokenExpiry =
                DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            string? resetLink =
                Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        token
                    },
                    Request.Scheme
                );

            string subject =
                "Reset Your Swapster Password";

            string body = $@"
                <h2>Reset Your Password</h2>

                <p>Hello {user.Username},</p>

                <p>
                    You requested to reset your Swapster password.
                </p>

                <p>
                    Click the button below to reset your password:
                </p>

                <p>
                    <a href='{resetLink}'
                       style='background:#6674ff;
                              color:white;
                              padding:12px 18px;
                              text-decoration:none;
                              border-radius:6px;
                              display:inline-block;'>

                        Reset Password
                    </a>
                </p>

                <p>
                    Or copy this link and open it in your browser:
                </p>

                <p>{resetLink}</p>

                <p>
                    This link will expire in 15 minutes.
                </p>

                <br />

                <p>
                    Regards,
                    <br />
                    Swapster Team
                </p>
            ";

            bool emailSent =
                _emailService.SendEmail(
                    email,
                    subject,
                    body
                );

            if (!emailSent)
            {
                ViewBag.Error =
                    "Email could not be sent. Check Gmail app password or SMTP settings.";

                return View();
            }

            ViewBag.Message =
                "Reset link has been sent to your email.";

            return View();
        }

        // =====================================================
        // RESET PASSWORD GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(
            string token)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(token))
            {
                return Content(
                    "Invalid or expired token."
                );
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.VerificationToken == token &&
                    item.TokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return Content(
                    "Invalid or expired token."
                );
            }

            ViewBag.Token = token;

            return View();
        }

        // =====================================================
        // RESET PASSWORD POST
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> ResetPassword(
            string token,
            string newPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return Content(
                    "Invalid password reset request."
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.VerificationToken == token &&
                    item.TokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return Content(
                    "Invalid or expired token."
                );
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword
                );

            user.VerificationToken = null;
            user.TokenExpiry = null;

            await _context.SaveChangesAsync();

            // Password reset se account unblock nahi hoga
            if (user.IsBlocked)
            {
                TempData["Error"] =
                    "Password reset successfully, but your account is blocked by the administrator.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            SetUserSession(user);

            TempData["Success"] =
                "Password reset successfully.";

            if (user.AccessLevel == 1)
            {
                return RedirectToAction(
                    "AdminDashboard",
                    "Home"
                );
            }

            return RedirectToAction(
                "Dashboard",
                "Home"
            );
        }

        // =====================================================
        // CHANGE PASSWORD GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            return View();
        }

        // =====================================================
        // CHANGE PASSWORD POST
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> ChangePassword(
            string oldPassword,
            string newPassword,
            string confirmPassword)
        {
            ViewData["HideNavbar"] = true;
            ViewData["BodyClass"] = "auth-page";

            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error =
                    "All fields are required.";

                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error =
                    "New password and confirm password do not match.";

                return View();
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    oldPassword
                );

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ViewBag.Error =
                    "Incorrect current password.";

                return View();
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword
                );

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Password changed successfully.";

            return RedirectToAction(
                "MyAccount",
                "Account"
            );
        }


        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            ViewData["HideNavbar"] = true;
            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "dashboard-page";

            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            ViewBag.IsEmailVerified =
            user.IsEmailVerified;

            // Current user's active offered skills
            var mySkills =
                await _context.UserSkills
                    .AsNoTracking()
                    .Where(skill =>
                        skill.UserId == user.UserId &&
                        skill.IsActive &&
                        !skill.IsDeleted)
                    .OrderByDescending(skill =>
                        skill.SkillId)
                    .ToListAsync();

            // Current user's active wanted skills
            var wantedSkills =
                await _context.UserWantedSkills
                    .AsNoTracking()
                    .Where(skill =>
                        skill.UserId == user.UserId &&
                        skill.IsActive &&
                        !skill.IsDeleted)
                    .OrderByDescending(skill =>
                        skill.CreatedAt)
                    .ToListAsync();

            // Other users' active offered skills
            var otherOfferedSkills =
                await _context.UserSkills
                    .AsNoTracking()
                    .Include(skill =>
                        skill.User)
                    .Where(skill =>
                        skill.UserId != user.UserId &&
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.User != null &&
                        !skill.User.IsBlocked)
                    .ToListAsync();

            // Other users' active wanted skills
            var otherWantedSkills =
                await _context.UserWantedSkills
                    .AsNoTracking()
                    .Include(skill =>
                        skill.User)
                    .Where(skill =>
                        skill.UserId != user.UserId &&
                        skill.IsActive &&
                        !skill.IsDeleted &&
                        skill.User != null &&
                        !skill.User.IsBlocked)
                    .ToListAsync();

            var myWantedNames =
                new HashSet<string>(
                    wantedSkills
                        .Where(skill =>
                            !string.IsNullOrWhiteSpace(
                                skill.SkillName))
                        .Select(skill =>
                            skill.SkillName.Trim()),
                    StringComparer.OrdinalIgnoreCase
                );

            var myOfferedNames =
                new HashSet<string>(
                    mySkills
                        .Where(skill =>
                            !string.IsNullOrWhiteSpace(
                                skill.SkillName))
                        .Select(skill =>
                            skill.SkillName.Trim()),
                    StringComparer.OrdinalIgnoreCase
                );

            var candidateUserIds =
                otherOfferedSkills
                    .Where(skill =>
                        myWantedNames.Contains(
                            skill.SkillName.Trim()))
                    .Select(skill =>
                        skill.UserId)
                    .Distinct()
                    .ToList();

            var recommendedProfiles =
                new List<ProfileRecommendationViewModel>();

            foreach (int candidateUserId in candidateUserIds)
            {
                var candidateOffered =
                    otherOfferedSkills
                        .Where(skill =>
                            skill.UserId == candidateUserId)
                        .ToList();

                var candidateWanted =
                    otherWantedSkills
                        .Where(skill =>
                            skill.UserId == candidateUserId)
                        .ToList();

                var candidateUser =
                    candidateOffered
                        .Select(skill => skill.User)
                        .FirstOrDefault(item =>
                            item != null)
                    ??
                    candidateWanted
                        .Select(skill => skill.User)
                        .FirstOrDefault(item =>
                            item != null);

                if (candidateUser == null)
                {
                    continue;
                }

                var theyTeach =
                    candidateOffered
                        .Where(skill =>
                            myWantedNames.Contains(
                                skill.SkillName.Trim()))
                        .Select(skill =>
                            skill.SkillName.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                var youTeach =
                    candidateWanted
                        .Where(skill =>
                            myOfferedNames.Contains(
                                skill.SkillName.Trim()))
                        .Select(skill =>
                            skill.SkillName.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                if (!theyTeach.Any())
                {
                    continue;
                }

                recommendedProfiles.Add(
                    new ProfileRecommendationViewModel
                    {
                        UserId =
                            candidateUser.UserId,

                        Username =
                            candidateUser.Username ?? "User",

                        TheyTeach =
                            string.Join(", ", theyTeach),

                        YouTeach =
                            youTeach.Any()
                                ? string.Join(", ", youTeach)
                                : "No mutual teaching skill yet",

                        MatchType =
                            youTeach.Any()
                                ? "Mutual Skill Match"
                                : "Learning Match"
                    }
                );
            }

            recommendedProfiles =
                recommendedProfiles
                    .OrderByDescending(profile =>
                        profile.MatchType ==
                        "Mutual Skill Match")
                    .ThenBy(profile =>
                        profile.Username)
                    .Take(8)
                    .ToList();

            var model =
                new MyAccountViewModel
                {
                    Username =
                        user.Username ?? "User",

                    Email =
                        user.Email ?? "Not available",

                    MySkills =
                        mySkills,

                    WantedSkills =
                        wantedSkills,

                    RecommendedProfiles =
                        recommendedProfiles
                };

            return View(model);
        }

        // =====================================================
        // UPDATE ACCOUNT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount(
      string username,
      string email)
        {
            ViewData["HideNavbar"] = true;
            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "dashboard-page";

            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            username =
                username?.Trim() ?? string.Empty;

            email =
                email?.Trim() ?? string.Empty;

            // =====================================================
            // BASIC VALIDATION
            // =====================================================

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] =
                    "Username and email are required.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            // =====================================================
            // DUPLICATE EMAIL CHECK
            // =====================================================

            bool emailExists =
                await _context.Users.AnyAsync(item =>
                    item.UserId != user.UserId &&
                    item.Email.ToLower() == email.ToLower());

            if (emailExists)
            {
                TempData["Error"] =
                    "This email is already used by another account.";

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            // Check whether email was actually changed
            bool emailChanged =
                !string.Equals(
                    user.Email?.Trim(),
                    email,
                    StringComparison.OrdinalIgnoreCase
                );

            user.Username =
                username;

            user.Email =
                email;

            // =====================================================
            // EMAIL CHANGE REQUIRES VERIFICATION AGAIN
            // =====================================================

            if (emailChanged)
            {
                user.IsEmailVerified =
                    false;

                user.EmailVerificationToken =
                    Guid.NewGuid().ToString("N");

                user.EmailVerificationTokenExpiry =
                    DateTime.UtcNow.AddHours(24);
            }

            await _context.SaveChangesAsync();

            // Update username and email in session
            SetUserSession(user);

            // =====================================================
            // SEND VERIFICATION EMAIL TO NEW EMAIL
            // =====================================================

            if (emailChanged)
            {
                string? verificationLink =
                    Url.Action(
                        "VerifyEmail",
                        "Account",
                        new
                        {
                            token =
                                user.EmailVerificationToken
                        },
                        Request.Scheme
                    );

                if (string.IsNullOrWhiteSpace(verificationLink))
                {
                    TempData["Error"] =
                        "Account updated, but the verification link could not be generated.";

                    return RedirectToAction(
                        "MyAccount",
                        "Account"
                    );
                }

                string subject =
                    "Verify Your New Swapster Email";

                string body = $@"
            <h2>Verify Your New Email</h2>

            <p>Hello {user.Username},</p>

            <p>
                Your Swapster email address has been changed
                to <strong>{user.Email}</strong>.
            </p>

            <p>
                Please verify your new email before publishing
                a skill swap entry.
            </p>

            <p>
                <a href='{verificationLink}'
                   style='
                       background:#6674ff;
                       color:white;
                       padding:12px 18px;
                       text-decoration:none;
                       border-radius:6px;
                       display:inline-block;
                       font-weight:bold;'>

                    Verify New Email
                </a>
            </p>

            <p>
                Or copy and open this link in your browser:
            </p>

            <p>{verificationLink}</p>

            <p>
                This verification link will expire in 24 hours.
            </p>

            <br />

            <p>
                Regards,
                <br />
                Swapster Team
            </p>
        ";

                bool emailSent =
                    _emailService.SendEmail(
                        user.Email,
                        subject,
                        body
                    );

                if (emailSent)
                {
                    TempData["Success"] =
                        "Account updated. A verification link has been sent to your new email.";
                }
                else
                {
                    TempData["Error"] =
                        "Account updated, but the verification email could not be sent. Use the resend verification option.";
                }

                return RedirectToAction(
                    "MyAccount",
                    "Account"
                );
            }

            // Email change nahi hui, sirf username update hua
            TempData["Success"] =
                "Account updated successfully.";

            return RedirectToAction(
                "MyAccount",
                "Account"
            );
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }
    }
}