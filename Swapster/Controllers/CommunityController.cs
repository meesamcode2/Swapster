using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swapster.Data;
using Swapster.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Swapster.Controllers
{
    public class CommunityController : Controller
    {
        private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private const long MaxVideoSize =
            50 * 1024 * 1024; // 50 MB

        private static readonly string[] AllowedVideoExtensions =
        {
            ".mp4",
            ".webm",
            ".ogg"
        };

        private static readonly string[] AllowedVideoContentTypes =
        {
            "video/mp4",
            "video/webm",
            "video/ogg"
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CommunityController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =====================================================
        // AUTHENTICATED ACTIVE USER
        // =====================================================

        private async Task<User?> GetAuthenticatedUserAsync()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return null;
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(item =>
                        item.UserId == userId.Value);

            if (user == null ||
                user.IsBlocked ||
                user.IsDeactivated)
            {
                HttpContext.Session.Clear();
                return null;
            }

            return user;
        }

        // =====================================================
        // COMMUNITY PAGE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var posts =
                await _context.Posts
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(post =>
                        post.User)
                    .Include(post =>
                        post.Likes)
                    .Include(post =>
                        post.Comments)
                        .ThenInclude(comment =>
                            comment.User)
                    .Include(post =>
                        post.Shares)
                    .Where(post =>
                        post.User != null &&
                        !post.User.IsBlocked &&
                        !post.User.IsDeactivated)
                    .OrderByDescending(post =>
                        post.CreatedAt)
                    .ToListAsync();

            return View(posts);
        }

        // =====================================================
        // CREATE POST
        // Text-only, image-only, or text + image
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? content,
            IFormFile? image,
            IFormFile? video)
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            string cleanedContent =
                content?.Trim() ?? string.Empty;

            if (cleanedContent.Length > 2000)
            {
                TempData["Error"] =
                    "Post text cannot exceed 2000 characters.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            bool hasImage =
                image != null &&
                image.Length > 0;

            bool hasVideo =
                video != null &&
                video.Length > 0;

            if (string.IsNullOrWhiteSpace(
                    cleanedContent) &&
                !hasImage &&
                !hasVideo)
            {
                TempData["Error"] =
                    "Write something or choose an image/video before posting.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            string? imagePath = null;
            string? physicalImagePath = null;

            string? videoPath = null;
            string? physicalVideoPath = null;

            if (hasImage)
            {
                if (image!.Length > MaxImageSize)
                {
                    TempData["Error"] =
                        "Image size cannot exceed 5 MB.";

                    return RedirectToAction(
                        "Dashboard",
                        "Home"
                    );
                }

                string extension =
                    Path.GetExtension(
                        image.FileName
                    ).ToLowerInvariant();

                string contentType =
                    image.ContentType
                        ?.ToLowerInvariant()
                    ?? string.Empty;

                if (!AllowedExtensions.Contains(
                        extension) ||
                    !AllowedContentTypes.Contains(
                        contentType))
                {
                    TempData["Error"] =
                        "Only JPG, JPEG, PNG and WEBP images are allowed.";

                    return RedirectToAction(
                        "Dashboard",
                        "Home"
                    );
                }

                string webRoot =
                    _environment.WebRootPath
                    ?? Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );

                string uploadFolder =
                    Path.Combine(
                        webRoot,
                        "uploads",
                        "posts"
                    );

                Directory.CreateDirectory(
                    uploadFolder
                );

                string fileName =
                    $"{Guid.NewGuid():N}{extension}";

                physicalImagePath =
                    Path.Combine(
                        uploadFolder,
                        fileName
                    );

                await using (
                    var stream =
                        new FileStream(
                            physicalImagePath,
                            FileMode.Create
                        ))
                {
                    await image.CopyToAsync(
                        stream
                    );
                }

                imagePath =
                    $"/uploads/posts/{fileName}";
            }


            if (hasVideo)
            {
                if (video!.Length > MaxVideoSize)
                {
                    if (!string.IsNullOrWhiteSpace(
                            physicalImagePath) &&
                        System.IO.File.Exists(
                            physicalImagePath))
                    {
                        System.IO.File.Delete(
                            physicalImagePath
                        );
                    }

                    TempData["Error"] =
                        "Video size cannot exceed 50 MB.";

                    return RedirectToAction(
                        "Dashboard",
                        "Home"
                    );
                }

                string videoExtension =
                    Path.GetExtension(
                        video.FileName
                    ).ToLowerInvariant();

                string videoContentType =
                    video.ContentType
                        ?.ToLowerInvariant()
                    ?? string.Empty;

                if (!AllowedVideoExtensions.Contains(
                        videoExtension) ||
                    !AllowedVideoContentTypes.Contains(
                        videoContentType))
                {
                    if (!string.IsNullOrWhiteSpace(
                            physicalImagePath) &&
                        System.IO.File.Exists(
                            physicalImagePath))
                    {
                        System.IO.File.Delete(
                            physicalImagePath
                        );
                    }

                    TempData["Error"] =
                        "Only MP4, WEBM and OGG videos are allowed.";

                    return RedirectToAction(
                        "Dashboard",
                        "Home"
                    );
                }

                string webRoot =
                    _environment.WebRootPath
                    ?? Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );

                string videoUploadFolder =
                    Path.Combine(
                        webRoot,
                        "uploads",
                        "posts",
                        "videos"
                    );

                Directory.CreateDirectory(
                    videoUploadFolder
                );

                string videoFileName =
                    $"{Guid.NewGuid():N}{videoExtension}";

                physicalVideoPath =
                    Path.Combine(
                        videoUploadFolder,
                        videoFileName
                    );

                await using (
                    var videoStream =
                        new FileStream(
                            physicalVideoPath,
                            FileMode.Create
                        ))
                {
                    await video.CopyToAsync(
                        videoStream
                    );
                }

                videoPath =
                    $"/uploads/posts/videos/{videoFileName}";
            }

            var post =
                new Post
                {
                    UserId =
                        currentUser.UserId,

                    Content =
                        string.IsNullOrWhiteSpace(
                            cleanedContent)
                            ? null
                            : cleanedContent,

                    ImagePath =
                        imagePath,

                    VideoPath =
                        videoPath,

                    CreatedAt =
                        DateTime.UtcNow
                };

            try
            {
                _context.Posts.Add(post);

                await _context.SaveChangesAsync();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(
                        physicalImagePath) &&
                    System.IO.File.Exists(
                        physicalImagePath))
                {
                    System.IO.File.Delete(
                        physicalImagePath
                    );
                }

                if (!string.IsNullOrWhiteSpace(
                        physicalVideoPath) &&
                    System.IO.File.Exists(
                        physicalVideoPath))
                {
                    System.IO.File.Delete(
                        physicalVideoPath
                    );
                }

                throw;
            }

            TempData["Success"] =
                "Post published successfully.";

            return RedirectToAction(
                "Dashboard",
                "Home"
            );
        }

        // =====================================================
        // LIKE / UNLIKE POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(
            int postId)
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            bool postExists =
                await _context.Posts
                    .AnyAsync(post =>
                        post.PostId == postId &&
                        !post.User.IsBlocked &&
                        !post.User.IsDeactivated);

            if (!postExists)
            {
                TempData["Error"] =
                    "This post is no longer available.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            var existingLike =
                await _context.PostLikes
                    .FirstOrDefaultAsync(like =>
                        like.PostId == postId &&
                        like.UserId ==
                            currentUser.UserId);

            if (existingLike == null)
            {
                _context.PostLikes.Add(
                    new PostLike
                    {
                        PostId = postId,
                        UserId =
                            currentUser.UserId,
                        CreatedAt =
                            DateTime.UtcNow
                    }
                );
            }
            else
            {
                _context.PostLikes.Remove(
                    existingLike
                );
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Dashboard",
                "Home",
                fragment: $"post-{postId}"
            );
        }

        // =====================================================
        // ADD COMMENT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(
            int postId,
            string? comment)
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            string cleanedComment =
                comment?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    cleanedComment))
            {
                TempData["Error"] =
                    "Comment cannot be empty.";

                return Redirect(
                    $"/Home/Dashboard#post-{postId}"
                );
            }

            if (cleanedComment.Length > 1000)
            {
                TempData["Error"] =
                    "Comment cannot exceed 1000 characters.";

                return Redirect(
                    $"/Home/Dashboard#post-{postId}"
                );
            }

            bool postExists =
                await _context.Posts
                    .AnyAsync(post =>
                        post.PostId == postId &&
                        !post.User.IsBlocked &&
                        !post.User.IsDeactivated);

            if (!postExists)
            {
                TempData["Error"] =
                    "This post is no longer available.";

                return RedirectToAction(
                    "Dashboard",
                    "Home"
                );
            }

            _context.PostComments.Add(
                new PostComment
                {
                    PostId =
                        postId,

                    UserId =
                        currentUser.UserId,

                    Content =
                        cleanedComment,

                    CreatedAt =
                        DateTime.UtcNow
                }
            );

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Dashboard",
                "Home",
                fragment: $"post-{postId}"
            );
        }

        // =====================================================
        // RECORD SHARE
        // Called by JavaScript before native share/copy-link.
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(
            int postId)
        {
            var currentUser =
                await GetAuthenticatedUserAsync();

            if (currentUser == null)
            {
                return Unauthorized();
            }

            bool postExists =
                await _context.Posts
                    .AnyAsync(post =>
                        post.PostId == postId &&
                        !post.User.IsBlocked &&
                        !post.User.IsDeactivated);

            if (!postExists)
            {
                return NotFound();
            }

            _context.PostShares.Add(
                new PostShare
                {
                    PostId =
                        postId,

                    UserId =
                        currentUser.UserId,

                    SharedAt =
                        DateTime.UtcNow
                }
            );

            await _context.SaveChangesAsync();

            int shareCount =
                await _context.PostShares
                    .CountAsync(share =>
                        share.PostId ==
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