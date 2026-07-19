using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using System.Text.RegularExpressions;

namespace COSMETICC.Controllers
{
    public class AdminBlogController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminBlogController(AppDbContext context)
        {
            _context = context;
        }

        // Helper: Generate SEO Slug from Title
        private string GenerateSlug(string title)
        {
            string slug = title.ToLower().Trim();
            // Replace accented Vietnamese chars
            slug = Regex.Replace(slug, "[áàảãạăắằẳẵặâấầẩẫậ]", "a");
            slug = Regex.Replace(slug, "[éèẻẽẹêếềểễệ]", "e");
            slug = Regex.Replace(slug, "[íìỉĩị]", "i");
            slug = Regex.Replace(slug, "[óòỏõọôốồổỗộơớờởỡợ]", "o");
            slug = Regex.Replace(slug, "[úùủũụưứừửữự]", "u");
            slug = Regex.Replace(slug, "[ýỳỷỹỵ]", "y");
            slug = Regex.Replace(slug, "đ", "d");
            // Remove special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            // Replace spaces with single dashes
            slug = Regex.Replace(slug, @"\s+", "-").Trim();
            // Limit length
            if (slug.Length > 200) slug = slug.Substring(0, 200);
            return slug;
        }

        // GET: AdminBlog
        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET: AdminBlog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();

            return View(post);
        }

        // GET: AdminBlog/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminBlog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost post)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(post.Slug))
                {
                    post.Slug = GenerateSlug(post.Title);
                }
                else
                {
                    post.Slug = GenerateSlug(post.Slug);
                }

                // Check slug duplicate
                var slugExist = await _context.BlogPosts.AnyAsync(p => p.Slug == post.Slug);
                if (slugExist)
                {
                    post.Slug += "-" + new Random().Next(1000, 9999);
                }

                post.CreatedAt = DateTime.Now;

                _context.BlogPosts.Add(post);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo bài viết mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: AdminBlog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();

            return View(post);
        }

        // POST: AdminBlog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost post)
        {
            if (id != post.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(post.Slug))
                    {
                        post.Slug = GenerateSlug(post.Title);
                    }
                    else
                    {
                        post.Slug = GenerateSlug(post.Slug);
                    }

                    // Check slug duplicate for other posts
                    var slugExist = await _context.BlogPosts.AnyAsync(p => p.Slug == post.Slug && p.Id != id);
                    if (slugExist)
                    {
                        post.Slug += "-" + new Random().Next(100, 999);
                    }

                    _context.BlogPosts.Update(post);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.BlogPosts.AnyAsync(p => p.Id == post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // POST: AdminBlog/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa bài viết thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
