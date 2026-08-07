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
    public class PostShareController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostShareController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session
                .GetInt32("UserId");
        }

        // =====================================================
        // USERS THAT ALREADY EXIST IN CURRENT USER'S MESSAGES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Recipients(
            int postId)
        {
            int? currentUserId =
                GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(
                    new
                    {
                        message =
                            "Please sign in again."
                    }
                );
            }

            var currentUser =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user =>
                        user.UserId ==
                        currentUserId.Value);

            if (currentUser == null ||
                currentUser.IsBlocked ||
                currentUser.IsDeactivated)
            {
                HttpContext.Session.Clear();

                return Unauthorized(
                    new
                    {
                        message =
                            "Your session is no longer active."
                    }
                );
            }

            bool postExists =
                await _context.Posts
                    .AsNoTracking()
                    .AnyAsync(post =>
                        post.PostId == postId);

            if (!postExists)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Post was not found."
                    }
                );
            }

            var conversationUserIds =
                await _context.Messages
                    .AsNoTracking()
                    .Where(message =>
                        message.SenderId ==
                            currentUserId.Value ||
                        message.ReceiverId ==
                            currentUserId.Value)
                    .Select(message =>
                        message.SenderId ==
                            currentUserId.Value
                            ? message.ReceiverId
                            : message.SenderId)
                    .Distinct()
                    .ToListAsync();

            var recipients =
                await _context.Users
                    .AsNoTracking()
                    .Where(user =>
                        conversationUserIds.Contains(
                            user.UserId
                        ) &&
                        user.UserId !=
                            currentUserId.Value &&
                        !user.IsBlocked &&
                        !user.IsDeactivated)
                    .OrderBy(user =>
                        user.Username)
                    .Select(user =>
                        new
                        {
                            userId =
                                user.UserId,

                            username =
                                user.Username
                        })
                    .ToListAsync();

            return Json(
                new
                {
                    recipients
                }
            );
        }

        // =====================================================
        // SHARE POST INTO AN EXISTING CONVERSATION
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            int postId,
            int receiverId)
        {
            int? currentUserId =
                GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message =
                            "Please sign in again."
                    }
                );
            }

            if (receiverId ==
                currentUserId.Value)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "You cannot share a post with yourself."
                    }
                );
            }

            var sender =
                await _context.Users
                    .FirstOrDefaultAsync(user =>
                        user.UserId ==
                        currentUserId.Value);

            if (sender == null ||
                sender.IsBlocked ||
                sender.IsDeactivated)
            {
                HttpContext.Session.Clear();

                return Unauthorized(
                    new
                    {
                        success = false,
                        message =
                            "Your session is no longer active."
                    }
                );
            }

            var receiver =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user =>
                        user.UserId ==
                        receiverId &&
                        !user.IsBlocked &&
                        !user.IsDeactivated);

            if (receiver == null)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message =
                            "This user is not available."
                    }
                );
            }

            var post =
                await _context.Posts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.PostId ==
                        postId);

            if (post == null)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message =
                            "Post was not found."
                    }
                );
            }

            bool conversationExists =
                await _context.Messages
                    .AsNoTracking()
                    .AnyAsync(message =>
                        (
                            message.SenderId ==
                                currentUserId.Value &&
                            message.ReceiverId ==
                                receiverId
                        )
                        ||
                        (
                            message.SenderId ==
                                receiverId &&
                            message.ReceiverId ==
                                currentUserId.Value
                        ));

            if (!conversationExists)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Start a conversation with this user before sharing a post."
                    }
                );
            }

            string sharedPostMarker =
                $"[[SHARED_POST:{postId}]]";

            var message =
                new Message
                {
                    SenderId =
                        currentUserId.Value,

                    ReceiverId =
                        receiverId,

                    Content =
                        sharedPostMarker,

                    SentAt =
                        DateTime.UtcNow
                };

            var share =
                new PostShare
                {
                    PostId =
                        postId,

                    UserId =
                        currentUserId.Value,

                    SharedAt =
                        DateTime.UtcNow
                };

            _context.Messages.Add(message);
            _context.PostShares.Add(share);

            await _context.SaveChangesAsync();

            int shareCount =
                await _context.PostShares
                    .AsNoTracking()
                    .CountAsync(item =>
                        item.PostId ==
                        postId);

            return Json(
                new
                {
                    success = true,
                    shareCount
                }
            );
        }
    }
}