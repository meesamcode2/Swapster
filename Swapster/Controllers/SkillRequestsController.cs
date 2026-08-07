using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class SkillRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillRequestController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // AUTHENTICATION HELPERS
        // =====================================================

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private async Task<User?> GetAuthenticatedUserAsync()
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return null;
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(item =>
                        item.UserId == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return null;
            }

            if (user.IsBlocked ||
                user.IsDeactivated)
            {
                HttpContext.Session.Clear();
                return null;
            }

            return user;
        }

        // =====================================================
        // SEND SKILL EXCHANGE REQUEST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            int receiverId,
            int skillId)
        {
            var sender =
                await GetAuthenticatedUserAsync();

            if (sender == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (sender.UserId ==
                receiverId)
            {
                TempData["Error"] =
                    "You cannot send a skill request to yourself.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            // Selected skill must still exist,
            // belong to receiver and be available.
            var skill =
                await _context.UserSkills
                    .AsNoTracking()
                    .Include(item =>
                        item.User)
                    .FirstOrDefaultAsync(item =>
                        item.SkillId ==
                            skillId &&
                        item.UserId ==
                            receiverId &&
                        item.IsActive &&
                        !item.IsDeleted &&
                        item.User != null &&
                        !item.User.IsBlocked &&
                        !item.User.IsDeactivated);

            if (skill == null)
            {
                TempData["Error"] =
                    "The selected skill is no longer available.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            // Same skill ke liye Pending ya Accepted
            // request dobara create nahi hogi.
            bool requestExists =
                await _context.SkillRequests
                    .AnyAsync(request =>
                        request.SkillId ==
                            skillId &&
                        (
                            request.Status ==
                                "Pending" ||
                            request.Status ==
                                "Accepted"
                        ) &&
                        (
                            (
                                request.SenderId ==
                                    sender.UserId &&
                                request.ReceiverId ==
                                    receiverId
                            )
                            ||
                            (
                                request.SenderId ==
                                    receiverId &&
                                request.ReceiverId ==
                                    sender.UserId
                            )
                        ));

            if (requestExists)
            {
                TempData["Error"] =
                    "A pending or active request already exists for this skill.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            var request =
                new SkillRequest
                {
                    SenderId =
                        sender.UserId,

                    ReceiverId =
                        receiverId,

                    SkillId =
                        skillId,

                    Status =
                        "Pending",

                    CreatedAt =
                        DateTime.UtcNow,

                    SenderConfirmedCompletion =
                        false,

                    ReceiverConfirmedCompletion =
                        false
                };

            _context.SkillRequests.Add(
                request
            );

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill exchange request sent successfully.";

            return RedirectToAction(
                "MyExchanges"
            );
        }

        // =====================================================
        // MY EXCHANGES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> MyExchanges()
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var requests =
                await _context.SkillRequests
                    .AsNoTracking()
                    .Include(request =>
                        request.Sender)
                    .Include(request =>
                        request.Receiver)
                    .Include(request =>
                        request.Skill)
                    .Where(request =>
                        request.SenderId ==
                            user.UserId ||
                        request.ReceiverId ==
                            user.UserId)
                    .OrderByDescending(request =>
                        request.CreatedAt)
                    .ToListAsync();

            return View(requests);
        }

        // =====================================================
        // ACCEPT REQUEST
        // Only receiver can accept Pending request
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(
            int id)
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request =
                await _context.SkillRequests
                    .FirstOrDefaultAsync(item =>
                        item.Id == id &&
                        item.ReceiverId ==
                            user.UserId &&
                        item.Status ==
                            "Pending");

            if (request == null)
            {
                TempData["Error"] =
                    "Request not found, already processed, or you are not authorized.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            request.Status =
                "Accepted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill exchange request accepted successfully.";

            return RedirectToAction(
                "MyExchanges"
            );
        }

        // =====================================================
        // REJECT REQUEST
        // Only receiver can reject Pending request
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id)
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request =
                await _context.SkillRequests
                    .FirstOrDefaultAsync(item =>
                        item.Id == id &&
                        item.ReceiverId ==
                            user.UserId &&
                        item.Status ==
                            "Pending");

            if (request == null)
            {
                TempData["Error"] =
                    "Request not found, already processed, or you are not authorized.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            request.Status =
                "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill exchange request rejected.";

            return RedirectToAction(
                "MyExchanges"
            );
        }

        // =====================================================
        // CONFIRM COMPLETION
        // Both sender and receiver must confirm
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            int id)
        {
            var user =
                await GetAuthenticatedUserAsync();

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request =
                await _context.SkillRequests
                    .FirstOrDefaultAsync(item =>
                        item.Id == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Skill exchange could not be found.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            bool isSender =
                request.SenderId ==
                user.UserId;

            bool isReceiver =
                request.ReceiverId ==
                user.UserId;

            if (!isSender &&
                !isReceiver)
            {
                TempData["Error"] =
                    "You are not authorized to update this exchange.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            if (request.Status !=
                "Accepted")
            {
                TempData["Error"] =
                    "Only an active exchange can be completed.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            if (isSender)
            {
                request.SenderConfirmedCompletion =
                    true;
            }

            if (isReceiver)
            {
                request.ReceiverConfirmedCompletion =
                    true;
            }

            if (request.SenderConfirmedCompletion &&
                request.ReceiverConfirmedCompletion)
            {
                request.Status =
                    "Completed";

                TempData["Success"] =
                    "Skill exchange completed successfully.";
            }
            else
            {
                TempData["Success"] =
                    "Your completion has been confirmed. Waiting for the other user.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "MyExchanges"
            );
        }
    }
}