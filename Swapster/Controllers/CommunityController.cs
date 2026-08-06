using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;

namespace Swapster.Controllers
{
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommunityController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        public async Task<IActionResult> Index()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string content)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Index");
            }

            var post = new Post
            {
                UserId = userId.Value,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}