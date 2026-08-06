using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class SkillRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
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
            int? senderId = GetCurrentUserId();

            if (senderId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // User apne aap ko request nahi bhej sakta
            if (senderId.Value == receiverId)
            {
                TempData["Error"] =
                    "You cannot send a skill request to yourself.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            // Check selected skill actually exists
            // and belongs to the receiver
            var skill = await _context.UserSkills
           .AsNoTracking()
           .FirstOrDefaultAsync(skill =>
              skill.SkillId == skillId &&
              skill.UserId == receiverId &&
              skill.IsActive &&
              !skill.IsDeleted);

            if (skill == null)
            {
                TempData["Error"] =
                    "The selected skill could not be found.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            // Same users ke darmiyan same skill ki
            // Pending ya Accepted request dobara na bane
            bool requestExists =
                await _context.SkillRequests.AnyAsync(r =>
                    r.SkillId == skillId &&
                    (
                        r.Status == "Pending" ||
                        r.Status == "Accepted"
                    ) &&
                    (
                        (
                            r.SenderId == senderId.Value &&
                            r.ReceiverId == receiverId
                        )
                        ||
                        (
                            r.SenderId == receiverId &&
                            r.ReceiverId == senderId.Value
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

            var request = new SkillRequest
            {
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                SkillId = skillId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                SenderConfirmedCompletion = false,
                ReceiverConfirmedCompletion = false
            };

            _context.SkillRequests.Add(request);

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
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var requests = await _context.SkillRequests
                .AsNoTracking()
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Include(r => r.Skill)
                .Where(r =>
                    r.SenderId == userId.Value ||
                    r.ReceiverId == userId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // =====================================================
        // ACCEPT REQUEST
        // Only receiver can accept a Pending request
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request = await _context.SkillRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.ReceiverId == userId.Value &&
                    r.Status == "Pending");

            if (request == null)
            {
                TempData["Error"] =
                    "Request not found, already processed, or you are not authorized.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            request.Status = "Accepted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Skill exchange request accepted successfully.";

            return RedirectToAction(
                "MyExchanges"
            );
        }

        // =====================================================
        // REJECT REQUEST
        // Only receiver can reject a Pending request
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request = await _context.SkillRequests
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.ReceiverId == userId.Value &&
                    r.Status == "Pending");

            if (request == null)
            {
                TempData["Error"] =
                    "Request not found, already processed, or you are not authorized.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            request.Status = "Rejected";

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
        public async Task<IActionResult> Complete(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var request = await _context.SkillRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Skill exchange could not be found.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            bool isSender =
                request.SenderId == userId.Value;

            bool isReceiver =
                request.ReceiverId == userId.Value;

            // Sirf exchange ke involved users confirm kar sakte hain
            if (!isSender && !isReceiver)
            {
                TempData["Error"] =
                    "You are not authorized to update this exchange.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            // Sirf Accepted exchange complete ho sakta hai
            if (request.Status != "Accepted")
            {
                TempData["Error"] =
                    "Only an active exchange can be completed.";

                return RedirectToAction(
                    "MyExchanges"
                );
            }

            if (isSender)
            {
                request.SenderConfirmedCompletion = true;
            }

            if (isReceiver)
            {
                request.ReceiverConfirmedCompletion = true;
            }

            // Dono users confirm kar dein to Completed
            if (request.SenderConfirmedCompletion &&
                request.ReceiverConfirmedCompletion)
            {
                request.Status = "Completed";

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