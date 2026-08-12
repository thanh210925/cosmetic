using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;

namespace COSMETICC.Controllers
{
    public class BlogController : Controller
    {
        private readonly AppDbContext _context;

        public BlogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Blog
        public async Task<IActionResult> Index(string? tag, string? search)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int? currentUserId = null;
            if (int.TryParse(userIdStr, out int parsedId))
            {
                currentUserId = parsedId;
            }

            var query = _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.BlogPostLikes)
                .Include(p => p.BlogPostComments)
                    .ThenInclude(c => c.User)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(cleanSearch) ||
                    (p.Summary != null && p.Summary.ToLower().Contains(cleanSearch)) ||
                    (p.Content != null && p.Content.ToLower().Contains(cleanSearch)) ||
                    (p.Tags != null && p.Tags.ToLower().Contains(cleanSearch))
                );
                ViewBag.SearchQuery = search;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                var cleanTag = tag.Trim().ToLower();
                query = query.Where(p => p.Tags != null && p.Tags.ToLower().Contains(cleanTag));
                ViewBag.CurrentTag = tag;
            }

            var posts = await query.ToListAsync();

            // Set current userId in ViewBag for frontend check
            ViewBag.CurrentUserId = currentUserId;

            // Load recent posts
            ViewBag.RecentPosts = await _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Unique tags list
            var allTags = await _context.BlogPosts
                .Where(p => p.IsPublished && !string.IsNullOrEmpty(p.Tags))
                .Select(p => p.Tags)
                .ToListAsync();

            var uniqueTags = allTags
                .SelectMany(t => t.Split(','))
                .Select(t => t.Trim())
                .Distinct()
                .Take(12)
                .ToList();

            ViewBag.UniqueTags = uniqueTags;

            return View(posts);
        }

        // POST: /Blog/CreatePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string title, string content, string? imageUrl, string? tags, string? summary, string? category, string? author)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đăng bài viết!";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Tiêu đề và nội dung bài viết không được để trống!";
                return RedirectToAction(nameof(Index));
            }

            // Simple slug generation
            string rawSlug = title.ToLower().Trim().Replace(" ", "-").Replace("đ", "d");
            string cleanSlug = new string(rawSlug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            string uniqueSlug = cleanSlug + "-" + DateTime.Now.Ticks.ToString().Substring(12);

            string finalTags = tags;
            if (!string.IsNullOrWhiteSpace(category))
            {
                finalTags = string.IsNullOrWhiteSpace(tags) ? category : $"{category}, {tags}";
            }

            var post = new BlogPost
            {
                Title = title.Trim(),
                Slug = uniqueSlug,
                Content = content.Trim(),
                Summary = !string.IsNullOrWhiteSpace(summary) ? summary.Trim() : (content.Length > 150 ? content.Substring(0, 147) + "..." : content),
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                Tags = string.IsNullOrWhiteSpace(finalTags) ? "Cộng đồng" : finalTags.Trim(),
                IsPublished = true,
                CreatedAt = DateTime.Now,
                UserId = userId,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0
            };

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng bài viết cẩm nang mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Blog/ToggleLike
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thích bài viết!" });
            }

            var post = await _context.BlogPosts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại!" });
            }

            var existingLike = await _context.BlogPostLikes
                .FirstOrDefaultAsync(l => l.BlogPostId == postId && l.UserId == userId);

            bool isLiked;
            if (existingLike != null)
            {
                _context.BlogPostLikes.Remove(existingLike);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
                isLiked = false;
            }
            else
            {
                var newLike = new BlogPostLike
                {
                    BlogPostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.BlogPostLikes.Add(newLike);
                post.LikesCount++;
                isLiked = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isLiked, likesCount = post.LikesCount });
        }

        // POST: /Blog/AddComment
        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để bình luận!" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Nội dung bình luận không được để trống!" });
            }

            var post = await _context.BlogPosts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại!" });
            }

            var comment = new BlogPostComment
            {
                BlogPostId = postId,
                UserId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.BlogPostComments.Add(comment);
            post.CommentsCount++;
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            string fullName = user?.FullName ?? user?.Username ?? "Khách hàng";
            string avatar = user?.Avatar ?? "/images/default-avatar.png";

            return Json(new
            {
                success = true,
                commentsCount = post.CommentsCount,
                comment = new
                {
                    comment.Id,
                    comment.Content,
                    CreatedAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UserFullName = fullName,
                    UserAvatar = avatar
                }
            });
        }

        // POST: /Blog/SharePost
        [HttpPost]
        public async Task<IActionResult> SharePost(int postId)
        {
            var post = await _context.BlogPosts.FindAsync(postId);
            if (post == null) return NotFound();

            post.SharesCount++;
            await _context.SaveChangesAsync();

            return Json(new { success = true, sharesCount = post.SharesCount });
        }

        // POST: /Blog/EditPost
        [HttpPost]
        public async Task<IActionResult> EditPost(int postId, string title, string content, string? imageUrl, string? tags)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để chỉnh sửa bài viết!" });
            }

            var post = await _context.BlogPosts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại!" });
            }

            // Kiểm tra quyền sở hữu bài viết
            if (post.UserId != userId)
            {
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa bài viết này!" });
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Tiêu đề và nội dung không được để trống!" });
            }

            post.Title = title.Trim();
            post.Content = content.Trim();
            post.Summary = content.Length > 150 ? content.Substring(0, 147) + "..." : content;
            post.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
            post.Tags = string.IsNullOrWhiteSpace(tags) ? "Cộng đồng" : tags.Trim();

            _context.BlogPosts.Update(post);
            await _context.SaveChangesAsync();

            return Json(new 
            { 
                success = true, 
                message = "Cập nhật bài viết thành công!",
                post = new 
                {
                    post.Id,
                    post.Title,
                    post.Content,
                    post.ImageUrl,
                    post.Tags
                }
            });
        }

        // POST: /Blog/DeletePost
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để xóa bài viết!" });
            }

            var post = await _context.BlogPosts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại!" });
            }

            // Kiểm tra quyền sở hữu
            if (post.UserId != userId)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết này!" });
            }

            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa bài viết thành công!" });
        }

        // GET: /Blog/Details/2 or /Blog/Details/my-article-slug
        public async Task<IActionResult> Details(string? id, string? slug)
        {
            var target = !string.IsNullOrEmpty(id) ? id : slug;
            if (string.IsNullOrEmpty(target))
            {
                return RedirectToAction(nameof(Index));
            }

            BlogPost? post = null;

            if (int.TryParse(target, out int numericId))
            {
                post = await _context.BlogPosts
                    .Include(p => p.User)
                    .Include(p => p.BlogPostLikes)
                    .Include(p => p.BlogPostComments)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(p => p.Id == numericId);
            }

            if (post == null)
            {
                post = await _context.BlogPosts
                    .Include(p => p.User)
                    .Include(p => p.BlogPostLikes)
                    .Include(p => p.BlogPostComments)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(p => p.Slug == target);
            }

            // Fallback: If requested post ID/slug does not exist in DB, load the latest published post
            if (post == null)
            {
                post = await _context.BlogPosts
                    .Include(p => p.User)
                    .Include(p => p.BlogPostLikes)
                    .Include(p => p.BlogPostComments)
                        .ThenInclude(c => c.User)
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (post == null)
            {
                TempData["ErrorMessage"] = "Chưa có bài viết nào trong hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            int? currentUserId = null;
            if (int.TryParse(userIdStr, out int parsedId))
            {
                currentUserId = parsedId;
            }
            ViewBag.CurrentUserId = currentUserId;

            // Load recent posts
            ViewBag.RecentPosts = await _context.BlogPosts
                .Where(p => p.IsPublished && p.Id != post.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(post);
        }
    }
}
