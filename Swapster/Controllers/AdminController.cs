using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // ADMIN AUTHORIZATION CHECK
        // =====================================================

        private bool IsAdmin()
        {
            int? accessLevel =
                HttpContext.Session.GetInt32("AccessLevel");

            return accessLevel == 1;
        }

        // =====================================================
        // ADMIN DASHBOARD ROUTE
        // =====================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            return RedirectToAction(
                "AdminDashboard",
                "Home"
            );
        }

        // =====================================================
        // USER MANAGEMENT
        // Search + Role Filter + Status Filter + Activity
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Users(
            string? searchTerm,
            string roleFilter = "all",
            string statusFilter = "all")
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            int currentAdminId =
                HttpContext.Session.GetInt32("UserId") ?? 0;

            string cleanedSearch =
                searchTerm?.Trim() ?? string.Empty;

            roleFilter =
                string.IsNullOrWhiteSpace(roleFilter)
                    ? "all"
                    : roleFilter.Trim().ToLower();

            statusFilter =
                string.IsNullOrWhiteSpace(statusFilter)
                    ? "all"
                    : statusFilter.Trim().ToLower();

            IQueryable<User> usersQuery =
                _context.Users.AsNoTracking();

            // Search user by username or email
            if (!string.IsNullOrWhiteSpace(cleanedSearch))
            {
                usersQuery = usersQuery.Where(user =>
                    user.Username.Contains(cleanedSearch) ||
                    user.Email.Contains(cleanedSearch));
            }

            // Role filter
            if (roleFilter == "admin")
            {
                usersQuery = usersQuery.Where(user =>
                    user.AccessLevel == 1);
            }
            else if (roleFilter == "user")
            {
                usersQuery = usersQuery.Where(user =>
                    user.AccessLevel != 1);
            }
            else
            {
                roleFilter = "all";
            }

            // Account status filter
            if (statusFilter == "active")
            {
                usersQuery = usersQuery.Where(user =>
                    !user.IsBlocked);
            }
            else if (statusFilter == "blocked")
            {
                usersQuery = usersQuery.Where(user =>
                    user.IsBlocked);
            }
            else
            {
                statusFilter = "all";
            }

            var filteredUsers = await usersQuery
                .OrderBy(user => user.UserId)
                .ToListAsync();

            // Used by the Admin Users view so self-deactivated accounts
            // can be shown as "Deactivated" with a "Reactivate" action,
            // while admin-blocked accounts still show "Blocked/Unblock".
            ViewBag.DeactivatedUserIds =
                filteredUsers
                    .Where(user => user.IsDeactivated)
                    .Select(user => user.UserId)
                    .ToHashSet();

            var userIds = filteredUsers
                .Select(user => user.UserId)
                .ToList();

            var offeredSkillsCounts =
                new Dictionary<int, int>();

            var wantedSkillsCounts =
                new Dictionary<int, int>();

            var completedExchanges =
                new List<CompletedExchangeCountItem>();

            if (userIds.Any())
            {
                offeredSkillsCounts =
                    await _context.UserSkills
                        .AsNoTracking()
                        .Where(skill =>
                            userIds.Contains(skill.UserId) &&
                            !skill.IsDeleted)
                        .GroupBy(skill => skill.UserId)
                        .Select(group => new
                        {
                            UserId = group.Key,
                            Count = group.Count()
                        })
                        .ToDictionaryAsync(
                            item => item.UserId,
                            item => item.Count
                        );

                wantedSkillsCounts =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .Where(skill =>
                            userIds.Contains(skill.UserId) &&
                            !skill.IsDeleted)
                        .GroupBy(skill => skill.UserId)
                        .Select(group => new
                        {
                            UserId = group.Key,
                            Count = group.Count()
                        })
                        .ToDictionaryAsync(
                            item => item.UserId,
                            item => item.Count
                        );

                completedExchanges =
                    await _context.SkillRequests
                        .AsNoTracking()
                        .Where(request =>
                            request.Status == "Completed" &&
                            (
                                userIds.Contains(request.SenderId) ||
                                userIds.Contains(request.ReceiverId)
                            ))
                        .Select(request =>
                            new CompletedExchangeCountItem
                            {
                                SenderId = request.SenderId,
                                ReceiverId = request.ReceiverId
                            })
                        .ToListAsync();
            }

            var userRows = filteredUsers
                .Select(user =>
                    new AdminUserRowViewModel
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        AccessLevel = user.AccessLevel,
                        IsEmailVerified =
                            user.IsEmailVerified,
                        IsBlocked =
                            user.IsBlocked,
                        CreatedAt =
                            user.CreatedAt,

                        OfferedSkillsCount =
                            offeredSkillsCounts.TryGetValue(
                                user.UserId,
                                out int offeredCount)
                                ? offeredCount
                                : 0,

                        WantedSkillsCount =
                            wantedSkillsCounts.TryGetValue(
                                user.UserId,
                                out int wantedCount)
                                ? wantedCount
                                : 0,

                        CompletedExchangesCount =
                            completedExchanges.Count(exchange =>
                                exchange.SenderId == user.UserId ||
                                exchange.ReceiverId == user.UserId)
                    })
                .ToList();

            var model = new AdminUsersViewModel
            {
                SearchTerm = cleanedSearch,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,
                CurrentAdminId = currentAdminId,

                TotalUsers =
                    await _context.Users.CountAsync(),

                ActiveUsers =
                    await _context.Users.CountAsync(user =>
                        !user.IsBlocked),

                BlockedUsers =
                    await _context.Users.CountAsync(user =>
                        user.IsBlocked),

                AdminUsers =
                    await _context.Users.CountAsync(user =>
                        user.AccessLevel == 1),

                Users = userRows
            };

            return View(model);
        }

        // =====================================================
        // USER DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(nameof(Users));
            }

            var offeredSkills =
                await _context.UserSkills
                    .AsNoTracking()
                    .Where(skill =>
                        skill.UserId == id)
                    .OrderBy(skill =>
                        skill.SkillName)
                    .ToListAsync();

            var wantedSkills =
                await _context.UserWantedSkills
                    .AsNoTracking()
                    .Where(skill =>
                        skill.UserId == id)
                    .OrderBy(skill =>
                        skill.SkillName)
                    .ToListAsync();

            var exchanges =
                await _context.SkillRequests
                    .AsNoTracking()
                    .Include(request =>
                        request.Skill)
                    .Include(request =>
                        request.Sender)
                    .Include(request =>
                        request.Receiver)
                    .Where(request =>
                        request.SenderId == id ||
                        request.ReceiverId == id)
                    .OrderByDescending(request =>
                        request.CreatedAt)
                    .ToListAsync();

            var model = new AdminUserDetailsViewModel
            {
                User = user,
                OfferedSkills = offeredSkills,
                WantedSkills = wantedSkills,
                Exchanges = exchanges,

                CurrentAdminId =
                    HttpContext.Session
                        .GetInt32("UserId") ?? 0,

                SentRequestsCount =
                    exchanges.Count(request =>
                        request.SenderId == id),

                ReceivedRequestsCount =
                    exchanges.Count(request =>
                        request.ReceiverId == id),

                CompletedExchangesCount =
                    exchanges.Count(request =>
                        request.Status == "Completed")
            };

            return View(model);
        }

        // =====================================================
        // BLOCK / UNBLOCK USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(
            int id,
            bool returnToDetails = false)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            int currentAdminId =
                HttpContext.Session.GetInt32("UserId") ?? 0;

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(nameof(Users));
            }

            if (user.UserId == currentAdminId)
            {
                TempData["Error"] =
                    "You cannot block your own administrator account.";

                return RedirectAfterUserAction(
                    user.UserId,
                    returnToDetails
                );
            }

            if (user.AccessLevel == 1)
            {
                TempData["Error"] =
                    "Administrator accounts cannot be blocked. Change the role to User first.";

                return RedirectAfterUserAction(
                    user.UserId,
                    returnToDetails
                );
            }

            user.IsBlocked =
                !user.IsBlocked;

            // If the user deactivated their own account,
            // unblocking from Admin also reactivates the account.
            if (!user.IsBlocked &&
                user.IsDeactivated)
            {
                user.IsDeactivated =
                    false;

                user.DeactivatedAt =
                    null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                user.IsBlocked
                    ? $"{user.Username} has been blocked."
                    : $"{user.Username} has been reactivated and unblocked.";

            return RedirectAfterUserAction(
                user.UserId,
                returnToDetails
            );
        }

        // =====================================================
        // CHANGE USER ROLE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            int id,
            bool returnToDetails = false)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            int currentAdminId =
                HttpContext.Session.GetInt32("UserId") ?? 0;

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserId == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(nameof(Users));
            }

            if (user.UserId == currentAdminId)
            {
                TempData["Error"] =
                    "You cannot change your own administrator role.";

                return RedirectAfterUserAction(
                    user.UserId,
                    returnToDetails
                );
            }

            if (user.AccessLevel == 1)
            {
                int totalAdmins =
                    await _context.Users.CountAsync(item =>
                        item.AccessLevel == 1);

                if (totalAdmins <= 1)
                {
                    TempData["Error"] =
                        "The system must have at least one administrator.";

                    return RedirectAfterUserAction(
                        user.UserId,
                        returnToDetails
                    );
                }

                user.AccessLevel = 2;

                TempData["Success"] =
                    $"{user.Username} is now a regular user.";
            }
            else
            {
                user.AccessLevel = 1;

                // Admin account cannot remain blocked or deactivated.
                user.IsBlocked = false;
                user.IsDeactivated = false;
                user.DeactivatedAt = null;

                TempData["Success"] =
                    $"{user.Username} has been promoted to administrator.";
            }

            await _context.SaveChangesAsync();

            return RedirectAfterUserAction(
                user.UserId,
                returnToDetails
            );
        }

        // =====================================================
        // SKILL MANAGEMENT
        // Search + Filters + Sorting
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Skills(
            string? searchTerm,
            string typeFilter = "all",
            int levelFilter = 0,
            string statusFilter = "all",
            string sortOrder = "latest")
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            string cleanedSearch =
                searchTerm?.Trim() ?? string.Empty;

            typeFilter =
                string.IsNullOrWhiteSpace(typeFilter)
                    ? "all"
                    : typeFilter.Trim().ToLower();

            statusFilter =
                string.IsNullOrWhiteSpace(statusFilter)
                    ? "all"
                    : statusFilter.Trim().ToLower();

            sortOrder =
                string.IsNullOrWhiteSpace(sortOrder)
                    ? "latest"
                    : sortOrder.Trim().ToLower();

            if (typeFilter != "all" &&
                typeFilter != "offered" &&
                typeFilter != "wanted")
            {
                typeFilter = "all";
            }

            if (statusFilter != "all" &&
                statusFilter != "active" &&
                statusFilter != "inactive" &&
                statusFilter != "deleted")
            {
                statusFilter = "all";
            }

            if (sortOrder != "latest" &&
                sortOrder != "oldest" &&
                sortOrder != "name")
            {
                sortOrder = "latest";
            }

            if (levelFilter < 0 ||
                levelFilter > 5)
            {
                levelFilter = 0;
            }

            IQueryable<UserSkill> offeredQuery =
                _context.UserSkills
                    .AsNoTracking()
                    .Include(skill => skill.User);

            IQueryable<UserWantedSkill> wantedQuery =
                _context.UserWantedSkills
                    .AsNoTracking()
                    .Include(skill => skill.User);

            // Search by skill, username or email
            if (!string.IsNullOrWhiteSpace(cleanedSearch))
            {
                offeredQuery = offeredQuery.Where(skill =>
                    skill.SkillName.Contains(cleanedSearch) ||
                    (
                        skill.User != null &&
                        (
                            skill.User.Username.Contains(cleanedSearch) ||
                            skill.User.Email.Contains(cleanedSearch)
                        )
                    ));

                wantedQuery = wantedQuery.Where(skill =>
                    skill.SkillName.Contains(cleanedSearch) ||
                    (
                        skill.User != null &&
                        (
                            skill.User.Username.Contains(cleanedSearch) ||
                            skill.User.Email.Contains(cleanedSearch)
                        )
                    ));
            }

            // Level filter
            if (levelFilter > 0)
            {
                offeredQuery = offeredQuery.Where(skill =>
                    skill.ProficiencyLevel == levelFilter);

                wantedQuery = wantedQuery.Where(skill =>
                    skill.DesiredLevel == levelFilter);
            }

            // Status filter
            if (statusFilter == "active")
            {
                offeredQuery = offeredQuery.Where(skill =>
                    skill.IsActive &&
                    !skill.IsDeleted);

                wantedQuery = wantedQuery.Where(skill =>
                    skill.IsActive &&
                    !skill.IsDeleted);
            }
            else if (statusFilter == "inactive")
            {
                offeredQuery = offeredQuery.Where(skill =>
                    !skill.IsActive &&
                    !skill.IsDeleted);

                wantedQuery = wantedQuery.Where(skill =>
                    !skill.IsActive &&
                    !skill.IsDeleted);
            }
            else if (statusFilter == "deleted")
            {
                offeredQuery = offeredQuery.Where(skill =>
                    skill.IsDeleted);

                wantedQuery = wantedQuery.Where(skill =>
                    skill.IsDeleted);
            }

            // Sorting
            if (sortOrder == "oldest")
            {
                offeredQuery =
                    offeredQuery.OrderBy(skill =>
                        skill.SkillId);

                wantedQuery =
                    wantedQuery.OrderBy(skill =>
                        skill.CreatedAt);
            }
            else if (sortOrder == "name")
            {
                offeredQuery =
                    offeredQuery.OrderBy(skill =>
                        skill.SkillName);

                wantedQuery =
                    wantedQuery.OrderBy(skill =>
                        skill.SkillName);
            }
            else
            {
                offeredQuery =
                    offeredQuery.OrderByDescending(skill =>
                        skill.SkillId);

                wantedQuery =
                    wantedQuery.OrderByDescending(skill =>
                        skill.CreatedAt);
            }

            var offeredSkills =
                new List<AdminOfferedSkillRowViewModel>();

            var wantedSkills =
                new List<AdminWantedSkillRowViewModel>();

            if (typeFilter != "wanted")
            {
                offeredSkills =
                    await offeredQuery
                        .Select(skill =>
                            new AdminOfferedSkillRowViewModel
                            {
                                SkillId =
                                    skill.SkillId,

                                SkillName =
                                    skill.SkillName,

                                Description =
                                    skill.Description ??
                                    string.Empty,

                                ProficiencyLevel =
                                    skill.ProficiencyLevel,

                                UserId =
                                    skill.UserId,

                                Username =
                                    skill.User != null
                                        ? skill.User.Username
                                        : "Unknown",

                                Email =
                                    skill.User != null
                                        ? skill.User.Email
                                        : "N/A",

                                IsActive =
                                    skill.IsActive,

                                IsDeleted =
                                    skill.IsDeleted,

                                AdminNote =
                                    skill.AdminNote,

                                RequestCount =
                                    _context.SkillRequests.Count(
                                        request =>
                                            request.SkillId ==
                                            skill.SkillId
                                    )
                            })
                        .ToListAsync();
            }

            if (typeFilter != "offered")
            {
                wantedSkills =
                    await wantedQuery
                        .Select(skill =>
                            new AdminWantedSkillRowViewModel
                            {
                                WantedSkillId =
                                    skill.WantedSkillId,

                                SkillName =
                                    skill.SkillName,

                                DesiredLevel =
                                    skill.DesiredLevel,

                                UserId =
                                    skill.UserId,

                                Username =
                                    skill.User != null
                                        ? skill.User.Username
                                        : "Unknown",

                                Email =
                                    skill.User != null
                                        ? skill.User.Email
                                        : "N/A",

                                CreatedAt =
                                    skill.CreatedAt,

                                IsActive =
                                    skill.IsActive,

                                IsDeleted =
                                    skill.IsDeleted,

                                AdminNote =
                                    skill.AdminNote
                            })
                        .ToListAsync();
            }

            var model = new AdminSkillsViewModel
            {
                SearchTerm = cleanedSearch,
                TypeFilter = typeFilter,
                LevelFilter = levelFilter,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,

                TotalOfferedSkills =
                    await _context.UserSkills
                        .CountAsync(skill =>
                            !skill.IsDeleted),

                TotalWantedSkills =
                    await _context.UserWantedSkills
                        .CountAsync(skill =>
                            !skill.IsDeleted),

                ActiveSkills =
                    await _context.UserSkills
                        .CountAsync(skill =>
                            skill.IsActive &&
                            !skill.IsDeleted)
                    +
                    await _context.UserWantedSkills
                        .CountAsync(skill =>
                            skill.IsActive &&
                            !skill.IsDeleted),

                InactiveSkills =
                    await _context.UserSkills
                        .CountAsync(skill =>
                            !skill.IsActive &&
                            !skill.IsDeleted)
                    +
                    await _context.UserWantedSkills
                        .CountAsync(skill =>
                            !skill.IsActive &&
                            !skill.IsDeleted),

                DeletedSkills =
                    await _context.UserSkills
                        .CountAsync(skill =>
                            skill.IsDeleted)
                    +
                    await _context.UserWantedSkills
                        .CountAsync(skill =>
                            skill.IsDeleted),

                OfferedSkills =
                    offeredSkills,

                WantedSkills =
                    wantedSkills
            };

            return View(model);
        }

        // =====================================================
        // SKILL DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SkillDetails(
            string type,
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            type =
                type?.Trim().ToLower() ??
                string.Empty;

            if (type == "offered")
            {
                var skill = await _context.UserSkills
                    .AsNoTracking()
                    .Include(item => item.User)
                    .FirstOrDefaultAsync(item =>
                        item.SkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var requests =
                    await _context.SkillRequests
                        .AsNoTracking()
                        .Include(request =>
                            request.Sender)
                        .Include(request =>
                            request.Receiver)
                        .Where(request =>
                            request.SkillId == id)
                        .OrderByDescending(request =>
                            request.CreatedAt)
                        .ToListAsync();

                var model =
                    new AdminSkillDetailsViewModel
                    {
                        SkillType = "offered",
                        SkillId = skill.SkillId,
                        SkillName = skill.SkillName,

                        Description =
                            skill.Description ??
                            string.Empty,

                        Level =
                            skill.ProficiencyLevel,

                        UserId =
                            skill.UserId,

                        Username =
                            skill.User?.Username ??
                            "Unknown",

                        Email =
                            skill.User?.Email ??
                            "N/A",

                        IsActive =
                            skill.IsActive,

                        IsDeleted =
                            skill.IsDeleted,

                        AdminNote =
                            skill.AdminNote,

                        TotalRequests =
                            requests.Count,

                        PendingRequests =
                            requests.Count(request =>
                                request.Status == "Pending"),

                        AcceptedRequests =
                            requests.Count(request =>
                                request.Status == "Accepted"),

                        RejectedRequests =
                            requests.Count(request =>
                                request.Status == "Rejected"),

                        CompletedRequests =
                            requests.Count(request =>
                                request.Status == "Completed"),

                        Exchanges =
                            requests
                    };

                return View(model);
            }

            if (type == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .Include(item =>
                            item.User)
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var model =
                    new AdminSkillDetailsViewModel
                    {
                        SkillType =
                            "wanted",

                        SkillId =
                            skill.WantedSkillId,

                        SkillName =
                            skill.SkillName,

                        Description =
                            string.Empty,

                        Level =
                            skill.DesiredLevel,

                        UserId =
                            skill.UserId,

                        Username =
                            skill.User?.Username ??
                            "Unknown",

                        Email =
                            skill.User?.Email ??
                            "N/A",

                        CreatedAt =
                            skill.CreatedAt,

                        IsActive =
                            skill.IsActive,

                        IsDeleted =
                            skill.IsDeleted,

                        AdminNote =
                            skill.AdminNote
                    };

                return View(model);
            }

            TempData["Error"] =
                "Invalid skill type.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // EDIT SKILL GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> EditSkill(
            string type,
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            type =
                type?.Trim().ToLower() ??
                string.Empty;

            if (type == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.SkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var model =
                    new AdminEditSkillViewModel
                    {
                        SkillType =
                            "offered",

                        SkillId =
                            skill.SkillId,

                        SkillName =
                            skill.SkillName,

                        Description =
                            skill.Description ??
                            string.Empty,

                        Level =
                            skill.ProficiencyLevel
                    };

                return View(model);
            }

            if (type == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var model =
                    new AdminEditSkillViewModel
                    {
                        SkillType =
                            "wanted",

                        SkillId =
                            skill.WantedSkillId,

                        SkillName =
                            skill.SkillName,

                        Description =
                            string.Empty,

                        Level =
                            skill.DesiredLevel
                    };

                return View(model);
            }

            TempData["Error"] =
                "Invalid skill type.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // EDIT SKILL POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(
            AdminEditSkillViewModel model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            model.SkillType =
                model.SkillType?.Trim().ToLower() ??
                string.Empty;

            model.SkillName =
                model.SkillName?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                model.SkillName))
            {
                ModelState.AddModelError(
                    nameof(model.SkillName),
                    "Skill name is required."
                );
            }

            if (model.Level < 1 ||
                model.Level > 5)
            {
                ModelState.AddModelError(
                    nameof(model.Level),
                    "Level must be between 1 and 5."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SkillType == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .FirstOrDefaultAsync(item =>
                            item.SkillId ==
                            model.SkillId);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.SkillName =
                    model.SkillName;

                skill.Description =
                    model.Description?.Trim() ??
                    string.Empty;

                skill.ProficiencyLevel =
                    model.Level;
            }
            else if (model.SkillType == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId ==
                            model.SkillId);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.SkillName =
                    model.SkillName;

                skill.DesiredLevel =
                    model.Level;
            }
            else
            {
                TempData["Error"] =
                    "Invalid skill type.";

                return RedirectToAction(nameof(Skills));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill updated successfully.";

            return RedirectToAction(
                nameof(SkillDetails),
                new
                {
                    type = model.SkillType,
                    id = model.SkillId
                }
            );
        }

        // =====================================================
        // ACTIVATE / DEACTIVATE SKILL
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSkillStatus(
            string type,
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            type =
                type?.Trim().ToLower() ??
                string.Empty;

            bool newStatus;
            string skillName;

            if (type == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .FirstOrDefaultAsync(item =>
                            item.SkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                if (skill.IsDeleted)
                {
                    TempData["Error"] =
                        "Deleted skill cannot be activated or deactivated.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsActive =
                    !skill.IsActive;

                newStatus =
                    skill.IsActive;

                skillName =
                    skill.SkillName;
            }
            else if (type == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                if (skill.IsDeleted)
                {
                    TempData["Error"] =
                        "Deleted skill cannot be activated or deactivated.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsActive =
                    !skill.IsActive;

                newStatus =
                    skill.IsActive;

                skillName =
                    skill.SkillName;
            }
            else
            {
                TempData["Error"] =
                    "Invalid skill type.";

                return RedirectToAction(nameof(Skills));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                newStatus
                    ? $"{skillName} has been activated."
                    : $"{skillName} has been deactivated.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // DELETE SKILL GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(
            string type,
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            type =
                type?.Trim().ToLower() ??
                string.Empty;

            if (type == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.SkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var model =
                    new AdminDeleteSkillViewModel
                    {
                        SkillType =
                            "offered",

                        SkillId =
                            skill.SkillId,

                        SkillName =
                            skill.SkillName
                    };

                return View(model);
            }

            if (type == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                var model =
                    new AdminDeleteSkillViewModel
                    {
                        SkillType =
                            "wanted",

                        SkillId =
                            skill.WantedSkillId,

                        SkillName =
                            skill.SkillName
                    };

                return View(model);
            }

            TempData["Error"] =
                "Invalid skill type.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // DELETE SKILL POST — SOFT DELETE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(
            AdminDeleteSkillViewModel model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            model.SkillType =
                model.SkillType?.Trim().ToLower() ??
                string.Empty;

            model.AdminNote =
                model.AdminNote?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                model.AdminNote))
            {
                ModelState.AddModelError(
                    nameof(model.AdminNote),
                    "Deletion reason is required."
                );

                return View(model);
            }

            if (model.SkillType == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .FirstOrDefaultAsync(item =>
                            item.SkillId ==
                            model.SkillId);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsDeleted = true;
                skill.IsActive = false;

                skill.AdminNote =
                    model.AdminNote;
            }
            else if (model.SkillType == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId ==
                            model.SkillId);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsDeleted = true;
                skill.IsActive = false;

                skill.AdminNote =
                    model.AdminNote;
            }
            else
            {
                TempData["Error"] =
                    "Invalid skill type.";

                return RedirectToAction(nameof(Skills));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{model.SkillName} has been removed.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // RESTORE DELETED SKILL
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreSkill(
            string type,
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            type =
                type?.Trim().ToLower() ??
                string.Empty;

            string skillName;

            if (type == "offered")
            {
                var skill =
                    await _context.UserSkills
                        .FirstOrDefaultAsync(item =>
                            item.SkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Offered skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsDeleted = false;
                skill.IsActive = true;
                skill.AdminNote = null;

                skillName =
                    skill.SkillName;
            }
            else if (type == "wanted")
            {
                var skill =
                    await _context.UserWantedSkills
                        .FirstOrDefaultAsync(item =>
                            item.WantedSkillId == id);

                if (skill == null)
                {
                    TempData["Error"] =
                        "Wanted skill was not found.";

                    return RedirectToAction(nameof(Skills));
                }

                skill.IsDeleted = false;
                skill.IsActive = true;
                skill.AdminNote = null;

                skillName =
                    skill.SkillName;
            }
            else
            {
                TempData["Error"] =
                    "Invalid skill type.";

                return RedirectToAction(nameof(Skills));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{skillName} has been restored.";

            return RedirectToAction(nameof(Skills));
        }

        // =====================================================
        // EXCHANGE MANAGEMENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Exchanges()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account"
                );
            }

            var exchanges =
                await _context.SkillRequests
                    .AsNoTracking()
                    .Include(request =>
                        request.Sender)
                    .Include(request =>
                        request.Receiver)
                    .Include(request =>
                        request.Skill)
                    .OrderByDescending(request =>
                        request.CreatedAt)
                    .ToListAsync();

            return View(exchanges);
        }

        // =====================================================
        // USER ACTION REDIRECT HELPER
        // =====================================================

        private IActionResult RedirectAfterUserAction(
            int userId,
            bool returnToDetails)
        {
            if (returnToDetails)
            {
                return RedirectToAction(
                    nameof(UserDetails),
                    new
                    {
                        id = userId
                    }
                );
            }

            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // USER MANAGEMENT COUNT HELPER
        // =====================================================

        private class CompletedExchangeCountItem
        {
            public int SenderId { get; set; }

            public int ReceiverId { get; set; }
        }
    }
}