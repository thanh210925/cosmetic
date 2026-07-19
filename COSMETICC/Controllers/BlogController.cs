using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

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
        public async Task<IActionResult> Index(string? tag)
        {
            var query = _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(tag))
            {
                var cleanTag = tag.Trim().ToLower();
                query = query.Where(p => p.Tags != null && p.Tags.ToLower().Contains(cleanTag));
                ViewBag.CurrentTag = tag;
            }

            var posts = await query.ToListAsync();

            // Load popular/recent posts side widget
            ViewBag.RecentPosts = await _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Unique tags currently in blog posts
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

        // GET: /Blog/Details/my-article-slug
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var post = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (post == null)
            {
                // Fallback: check if slug is actually ID
                if (int.TryParse(slug, out int id))
                {
                    post = await _context.BlogPosts.FindAsync(id);
                }
            }

            if (post == null || !post.IsPublished)
            {
                return NotFound();
            }

            // Load popular/recent posts side widget
            ViewBag.RecentPosts = await _context.BlogPosts
                .Where(p => p.IsPublished && p.Id != post.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(post);
        }
    }
}
